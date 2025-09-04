---
hide:
  - toc
---

## Query Overview (DuckDB integration)

After prototyping several query systems for MnemonicDB, the usage of DuckDB was tested and surprisingly it cleanly offered
the largest set of features for the purpose of this project.

A short description of DuckDB is in order. DuckDB is a project started in 2018 by several researchers in the EU,
The project started with the main query system of PostgreSQL, but stripped out most of the storage and server specific components.
In its place the developers implemented a clean C++/C api and focused on single-machine performance. Several years later,
the core execution engine was rewritten to follow a "vectorized chunk" approach. 

It should be noted that DuckDB is an in-process OLAP database and not a client/server system, this means many "recommended practices"
for client/server systems are not applicable. For example, transferring data into DuckDB is extremely fast and operates at the
speed of `memcpy`. Likewise, reading data out of DuckDB can be copy-free in some cases.

### Vectorized Chunks
In this model data for queries is stored in vectors (arrays) of a fixed size (normally 2048 values) and the engine combines sets of these
vectors into a "chunk". The entire system operates on these chunks and thus they provide a very efficient way to load, process,
and transfer large amounts of data. 

A good example of how well this system works are "scalar" functions, imaging a query like `SELECT MAX(X, Y) FROM tbl` in this case
the engine will load a chunk for `X` and `Y` and then hand those chunks to the `MAX` function. This function is offered
two vectors of 2048 values as input and a single output vector. The `MAX` function then needs only to compare each pair 
of values and emit the result into the output vector. It's very trivial at this point to optimize `MAX` to use SIMD instructions.

### DuckDB API Features
The real power of DuckDB comes from its API. The input to DuckDB is often a `TableFunction` this is a specialized callback
that DuckDB will invoke to load data into chunks from a given source. These functions even have APIs for projection push-down 
(partial row selection) and parallel execution. DuckDB handles all the parallelism, thread pooling, string management, and 
work partitioning. The Table function only needs to say how many threads should call the scan function and DuckDB will call
this function in parallel until all threads return a result set of no results. 

When the engine finishes processing a chunk of results it will be offered to the query caller as the first chunk. It is possible
to run the engine in a "streaming" mode where the engine will not execute the entire query, but will only return the first chunk
of results, waiting for the caller to request the next chunk. This is useful for large queries that may not fit into memory. This
mode has a performance penalty however, so it is not the default. 

As mentioned the result returned by the engine is a chunk of value vectors. This means that a properly coded caller need not
allocate a row object for each row returned. Instead the caller can use flyweight iterators to process the results.


## Integration with MnemonicDB
MnemonicDB integrates with DuckDB through the `HyperDuck` library. This library is designed as a thin, high performance wrapper
over the DuckDB C API. It uses modern PInvoke methods to reduce the call overhead to be as low as possible. However, since DuckDB
deals with vectors at almost every level of the system, the overhead of these calls is trivial. 

HyperDuck offers a few advanced features not found in other DuckDB wrappers:

### Raw data interface for TableFunctions
HyperDuck offers `ref struct` level wrappers over the TableFunction APIs, and the structures that need to be filled out by 
table functions are presented to the caller as a `Span<T>` of values. This means that at times users will have to write a bit more
code, but the performance benefits are significant.

### Zero allocation projection for queries
The Adaptor subsystem of HyperDuck may appear complex, but it solves a critical problem, how to transfer results into C# memory 
from DuckDB as efficiently as possible. The Adaptor subsystem consists of 3 sub-sets of adaptors. These adaptors all take a 
DuckDB structure and push those results into a C# object. The keyword here is `push` not `create`. The adaptors accept previously 
existing structures, and are capable of updating that collection to include new data. This means that querying data into a List
will not recreate the List, but will instead resize and fill in the new data.

The three types of adaptors are:
* Result - This is an adaptor that takes a DuckDB result (a series of chunks) and pushes them into some C# object
* Row - This adaptor takes a single tuple of values from a result row, and pushes them into a C# object
* Value - This adaptor takes a single cell of a row and projects it into a C# object

Note: DuckDB offers a fairly complex object system. So it is possible to write a query that returns rows of lists of lists of lists
of integers. HyperDuck deals with this by bouncing the converters between the Row and Value adaptors. Each element in a list is considered
a sub row. 

All the adaptors are implemented as generic static methods on static classes. This means the C# inliner will be able to optimize 
this code to a surprising degree.


## Usage
In MnemonicDB, the best way to execute a query is to use the `.Query` method that appears on the Collection and IDb clases. Note: this
query engine is not scoped to the connected Connection or Db, it is simply a way to get quick access to the query engine anywere in the system. 
