#ifndef NEXUSMODS_MNEMONICDB_NATIVE_LIBRARY_H
#define NEXUSMODS_MNEMONICDB_NATIVE_LIBRARY_H

extern "C" {
    /**
     * This function opens a mnemonic database at the specified path.
     * If in_memory is true, the database will be created in memory.
     * If readonly is true, the database will be opened in read-only mode.
     *
     * Returns a pointer to the underlying RocksDB database object.
     */
    void* mnemonicdb_open(const char* path, bool in_memory, bool readonly);
}

#endif //NEXUSMODS_MNEMONICDB_NATIVE_LIBRARY_H