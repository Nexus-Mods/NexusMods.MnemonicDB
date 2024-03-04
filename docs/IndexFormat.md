---
hide:
  - toc
---

## Index Format

The index format of the framework follows fairly closely to one found in Datomic, although differences likely exist due it having
a different set of constraints and requirements. The base format of the system is a sorted set of tuples. However each tuple
exists in multiple indexes with a different sorting and conflict resolution strategy for each.

In general data flows through 4 core indexes:

* TxIndex - A sorted list of all transactions, this isn't a index per-se, but it is the primary source of truth. It's key-value lookup where
the key is the transaction id and the value a block of all the tuples in that transaction. The storage system is expected to be able to
store new blocks, get a block by id, and to get the highest id in the store. Based on this the system can build the rest of indexes.
* InMemory - This is a single block of all the tuples that have been added to the TxLog but not yet been merged with the other indexes
* Historical index - This is a large index of every tuple that has ever been added to the system. Naturally this means that a search for a value
asof a given T value can be done by searching for a matching tuple with a tx value equal to or less than a given T. This means that
at times finding the recent value may be O(n) where n is the number of transactions for the given attribute.
* Current index - This index is a sorted list of all the current value of every attribute for every entity. There isn't historical data here,
but the Txvalue is recorded for each tuple so that queries can filter out values that are newer than the query time. This index is built
under the assumption that the vast number of queries will be on a relatively recent basis time.

When we say the tuples are "sorted" the next question is "sorted by what?" The answer is that the InMemory and Historical and current
indexes are sorted in several ways, so that queries can efficently find the data they need.

* EATV - This index is used to find all the tuples for a given entity. Most often used for questions of "what is the state of this entity?"
* AETV - This index is used to find all the tiples for a given attribute. Most often used for questions of "what entities have this attribute?"
* AVTE - This index is used to find all the entities for a given attribute and value. Most often used for questions of "what entities have this attribute with this value?", this index
can be expensive to maintain, so it is opt-in, but enabled by default for attributes where the type of the attribute is a reference to another entity.
* VATE - This index is used to find all the attributes or entities that have a given value. Most often used for backreference queries such as "what entities point to this entity?", to save space
this index is also opt-in, but enabled by default for attributes where the type of the attribute is a reference to another entity.

## Data Structure

The basic storage system of the the system is a "block" of data. This block is a sorted set
of tuples. Taking a page out of the book of DuckDB, the tuples themselves are stored as a set
of columns. This means that all the entity Ids are located in memory together, all the attribute
ids together, etc. Values are heterogenous, so take a special type of column, a "blob" column. This
column itself contains two 32bit integer columns (offset and size) and a chunk of memory for value storage.

Taking another page from DuckDB's book, the rather large memory requirements of this storage format (32 bytes minimum
for a tuple), we can perform lightweight compression on this data. Chances are that a given block will only contain
a few attributes, or have data from a few Entity IDs, also that's likely that this data may be sorted by some of these
values, so many of the values may be repeated or close in value. Armed this we can "pack" these columns into
constants, dictionaries or min-max encoding.

Data then exists in two states: appendable and packed. As one would expect, appendable data can be modified
while packed blocks have been compressed with lightweight compression and must be unpacked before they can be
modified. In practice these blocks can never be modified, and instead a new appendable block is created by ingesting
the data from a packed block, and from there the data is sorted, appended, and then repacked.

As one would expect, this data exists in a linear format, and so is prone to many O(n) complexity issues as well
as requiring ever increasing amounts of memory. Thus at some point, blocks are split and formed into a tree.


## Index tree
When we say "index" what we often mean (in the case of the historical and current index) is that the data is broken
up into multiple blocks of a given size (often something like 8k tuples) and organized into a B+ tree.

In this index format, the leaf nodes are all standard packed datablocks (linear arrays of tuples stored in columns).
Interestingly, the index branches are also packed data blocks but with a different meaning to the data stored them,
and with extra columns. These nodes are called "index nodes".

The index nodes all contain the last tuple of each child node, and sort these tuples in the same format as the children. The
nodes then also contain 3 extra columns:

* child count - a count of the total number of datoms in the child node
* child node count - a count of the number of child nodes in the child node
* storage reference id - a unique id for the child node.

Since index nodes act like data nodes, many of the algorithms used for data nodes also apply to index nodes. For example,
a binary search of the contents of the index node will tell you what child node to look in for a specific tuple.

Index nodes are also packed, and follow the same concept of needing to be "unpacked" before they can be modified. In practice
this isn't an issue as the nodes are only updated when the in-memory index reaches a certain size. Index nodes also split
after a certain branch size, often 8k tuples. Currently the deletion of data is not supported by this system, so the chances
of a node needing to be rebalanced is low. Instead nodes are expanded until they need to be split, then they the split nodes are
merged into the parent, and when the parent reaches a given size it is also split and merged into its parent.

Since the overall branching factor is extremely high, a 8k branching factor means that 549 billion tuples
can exist in a single index before the depth increases above 3 nodes. This is extremely high and not supported, as the resulting
dataset would likely be over 18TB.

These nodes are all immutable after being packed, so the common update format for the index is to perform a sorted merge of an
index and a new appendable node of data. This results in a new index root node, and future queries can point to this node.

Note: since the current index is filtered by T, previous versions of indexes are never read again after the last query against
a given version of the index has finished. Thus old blocks are free to be GC'd by a background process has proven no reader
is reading from them. This garbage collection process can easily be made non-blocking and lazy and need not run often.

## Vectorized Querying
A simplistic way to query this data is to leverage the fact that every column and every block supports a indexed value lookup.
Blocks report their total number of datoms, and getting the datom at a given index is O(log n) where n is so low that it approaches
the performance of O(1). This means that getting a datom at a given index is very fast, and due to that we can easily binary search
an index to get to a specific datom, from there we can increment and decrement the index to get datoms around the target datom.

However, in practice this involves a lot of random access for data that we likely will want to bulk process. Thus we take yet another
page from DuckDB's book and support vectorized iterators.

In this approach, data is iterated over in small compact chunks. Think of these chunks as subsections of the column's data stored
as columns themselves, but this time they are always unpacked. This is also useful for columns that may store data in a binary packed
or dictionary format, as it can allow us to bulk process column data and keep more state on the stack during the unpacking process.

A nice side effect of using chunked iterators is that it allows us to leverage vector processing (and perhaps GPU processing) to
process these queries. For example, when looking for all datoms with a given entity ID, we can look at a single array of ulongs and
filter out the ones that do not match. In order to perform this filtering in a non-branching manner, we use another feature
from DuckDB and store a "mask" of live datoms as a small array of ulongs. If a given filter or processor of the chunk wants to mark
the datom as not matching, it simply sets the bit for that column to 0. Future processors can use this mask to either reduce work
performed, or to filter out results of processing. With all this in play a modern processor can easily process several datoms
at a time, without the need to perform conditional checks thanks to vectorized operations.














































