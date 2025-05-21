cmake_minimum_required(VERSION 3.31)
project(NexusMods_MnemonicDB_Native)

set(CMAKE_CXX_STANDARD 20)
find_package(RocksDB CONFIG REQUIRED)
add_library(NexusMods_MnemonicDB_Native SHARED library.cpp
        datom.h
        Comparers.h)
target_link_libraries(NexusMods_MnemonicDB_Native PRIVATE RocksDB::rocksdb RocksDB::rocksdb-shared)

