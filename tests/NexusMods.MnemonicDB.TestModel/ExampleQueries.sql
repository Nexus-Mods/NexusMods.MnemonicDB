-- namespace: NexusMods.MnemonicDB.TestModel

CREATE MACRO FilesForLoadout(loadoutId, db) AS TABLE
       SELECT Id, Path FROM mdb_File(Db=>db) WHERE LoadoutId = loadoutId;
