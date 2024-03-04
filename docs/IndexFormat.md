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
