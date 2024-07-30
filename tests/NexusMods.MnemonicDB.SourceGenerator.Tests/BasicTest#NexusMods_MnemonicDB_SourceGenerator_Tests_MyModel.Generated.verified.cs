//HintName: NexusMods_MnemonicDB_SourceGenerator_Tests_MyModel.Generated.cs
#nullable enable



namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

using global::System;
using global::System.Linq;
using global::System.Collections;
using global::System.Collections.Generic;
using global::TransparentValueObjects;

using global::NexusMods.MnemonicDB.Abstractions.Query;
using global::System.Reactive.Linq;
using global::DynamicData;
using global::Microsoft.Extensions.DependencyInjection;
using global::NexusMods.MnemonicDB.Abstractions;
using global::System.Diagnostics.CodeAnalysis;
using global::System.Diagnostics.Contracts;

using __ABSTRACTIONS__ = NexusMods.MnemonicDB.Abstractions;
using __MODELS__ = NexusMods.MnemonicDB.Abstractions.Models;
using __SEGMENTS__ = NexusMods.MnemonicDB.Abstractions.IndexSegments;
using __DI__ = Microsoft.Extensions.DependencyInjection;
using __COMPARERS__ = NexusMods.MnemonicDB.Abstractions.ElementComparers;



public partial class MyModel : __MODELS__.IModelFactory<MyModel, MyModel.ReadOnly>
{

    

    #region CRUD Methods


    /// <summary>
    /// A list of all required attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] RequiredAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name,
    };

    /// <summary>
    /// The primary attribute of the model, this really just means one of the required attributes of the model
    /// if an entity has this attribute, it is considered a valid entity.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute PrimaryAttribute => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name;

    /// <summary>
    /// A list of all attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] AllAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name,
    };

    /// <summary>
    /// Returns all MyModel entities in the database.
    /// </summary>
    public static __SEGMENTS__.Entities<MyModel.ReadOnly> All(__ABSTRACTIONS__.IDb db) {
        return db.Datoms(PrimaryAttribute).AsModels<MyModel.ReadOnly>(db);
    }


    /// <summary>
    /// Loads a model from the database, not providing any validation
    /// </summary>
    public static ReadOnly Load(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id) {
        return new ReadOnly(db, id);
    }

    /// <summary>
    /// Observe a model from the database, the stream ends when the model is deleted or otherwise invalidated
    /// </summary>
    public static IObservable<ReadOnly> Observe(IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(id)
            .QueryWhenChanged(d => new ReadOnly(conn.Db, id))
            .TakeWhile(model => model.IsValid());
    }

    /// <summary>
    /// Observe all models of this type from the database
    /// </summary>
    public static IObservable<DynamicData.IChangeSet<ReadOnly>> ObserveAll(IConnection conn)
    {
        return conn.ObserveDatoms(PrimaryAttribute)
            .Transform(d => Load(conn.Db, d.E));
    }

    /// <summary>
    /// Tries to get the entity with the given id from the database, if the model is invalid, it returns false.
    /// </summary>
    public static bool TryGet(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id, [NotNullWhen(true)] out MyModel.ReadOnly? result)
    {
        var model = Load(db, id);
        if (model.IsValid()) {
            result = model;
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// Assumes that the ids given point to MyModel entities, and
    /// returns a list of ReadOnly models of the entities.
    /// </summary>
    public static IEnumerable<MyModel.ReadOnly> Load(__ABSTRACTIONS__.IDb db, IEnumerable<__ABSTRACTIONS__.EntityId> ids) {
        return ids.Select(id => new MyModel.ReadOnly(db, id));
    }

    #endregion


    public partial class New : __MODELS__.ITemporaryEntity, __MODELS__.IHasEntityId {


    
    public New(__ABSTRACTIONS__.ITransaction tx) : base() {
        Id = tx.TempId();
        tx.Attach(this);
    }
    
    public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.PartitionId partition) : base() {
        Id = tx.TempId(partition);
        tx.Attach(this);
    }
    

        public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.EntityId eid) : base() {
            Id = eid;
            tx.Attach(this);
        }


        public void AddTo(__ABSTRACTIONS__.ITransaction tx)
        {
            tx.Add(Id, NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name, Name, false);


        }

        /// <summary>
        /// Implicit conversion from the model to the entity id.
        /// </summary>
        public static implicit operator __ABSTRACTIONS__.EntityId(MyModel.New model) {
            return model.Id;
        }

        /// <summary>
        /// Implicit conversion from the model to the model id.
        /// </summary>
        public static implicit operator MyModelId(MyModel.New model) {
            return model.MyModelId;
        }

        /// <summary>
        /// The entity id of the model as a model id.
        /// </summary>
        public MyModelId MyModelId => MyModelId.From(Id);

        /// <inheritdoc />
        public __ABSTRACTIONS__.EntityId Id { get; private set; }

        #region Attributes
        
        public required string Name { get; set; }
        #endregion
    }

    
    public readonly partial struct ReadOnly :
        __MODELS__.IReadOnlyModel<MyModel.ReadOnly> {

           /// <summary>
           /// The database segment containing the datoms for this entity in EAVT order.
           /// </summary>
           public readonly __SEGMENTS__.IndexSegment IndexSegment;

           public ReadOnly(__ABSTRACTIONS__.IDb db, __SEGMENTS__.IndexSegment segment, __ABSTRACTIONS__.EntityId id) {
               Db = db;
               Id = id;
               IndexSegment = segment;
           }

           /// <summary>
           /// Constructs a new ReadOnly model of the entity.
           /// </summary>
           public ReadOnly(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id) :
               this(db, db.Get(id), id)
           {
           }

           /// <summary>
           /// The entity id of the model.
           /// </summary>
           public __ABSTRACTIONS__.EntityId Id { get; }

           /// <inheritdoc />
           public MyModelId MyModelId => MyModelId.From(Id);

           /// <summary>
           /// The database that the entity is associated with.
           /// </summary>
           public __ABSTRACTIONS__.IDb Db { get; }

           /// <summary>
           /// Rebases the entity to the most recent version of the database
           /// </summary>
           public ReadOnly Rebase() => new ReadOnly(Db.Connection.Db, Id);

           /// <summary>
           /// Constructs a new ReadOnly model of the entity.
           /// </summary>
           public static ReadOnly Create(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id) {
               return new ReadOnly(db, db.Get(id), id);
           }

           /// <inheritdoc />
           public int Count => IndexSegment.Count;


           /// <inheritdoc />
           public IEnumerator<IReadDatom> GetEnumerator()
           {
               for (var i = 0; i < IndexSegment.Count; i++)
               {
                   yield return IndexSegment[i].Resolved;
               }
           }

           IEnumerator IEnumerable.GetEnumerator()
           {
               return GetEnumerator();
           }


           /// <summary>
           /// Looks for the given attribute in the entity
           /// </summary>
           public bool Contains(IAttribute attribute)
           {
               foreach (var datom in this)
               {
                   if (datom.A == attribute)
                       return true;
               }

               return false;
           }

           public override string ToString()
           {
               return "MyModel<" + Id + ">";
           }

           public bool IsValid()
           {
               // This is true when the struct is a default value.
               if (Db == null) return false;

               return this.Contains(PrimaryAttribute);
           }


           public string Name => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name.Get(this);





           /// <summary>
           /// Reloads the entity from the given database, essentially
           /// refreshing the entity.
           /// </summary>
           [Pure]
           public ReadOnly Rebase(__ABSTRACTIONS__.IDb db) => new ReadOnly(db, Id);

           /// <summary>
           /// Implicit conversion from the model to the entity id.
           /// </summary>
           public static implicit operator __ABSTRACTIONS__.EntityId(MyModel.ReadOnly model) {
               return model.Id;
           }

           public static implicit operator MyModelId(MyModel.ReadOnly? model) {
               return MyModelId.From(model!.Value.Id);
           }
        }
}

/// <summary>
/// A value object representing the id of a MyModel entity.
/// </summary>
public readonly partial struct MyModelId : IEquatable<MyModelId>, IEquatable<__ABSTRACTIONS__.EntityId>
{
    public readonly EntityId Value;

    public MyModelId(EntityId id) => Value = id;

    /// <summary>
    /// Constructs a new MyModelId from the given entity id.
    /// </summary>
    public static MyModelId From(__ABSTRACTIONS__.EntityId id) => new MyModelId(id);

    /// <summary>
    /// Constructs a new MyModelId from the given ulong.
    /// </summary>
    public static MyModelId From(ulong id) => new MyModelId(__ABSTRACTIONS__.EntityId.From(id));

    public static implicit operator EntityId(MyModelId id) => id.Value;
    public static implicit operator MyModelId(EntityId id) => MyModelId.From(id);


    public bool Equals(MyModelId other)
    {
        return Value.Value == other.Value.Value;
    }


    public bool Equals(__ABSTRACTIONS__.EntityId other)
    {
        return Value.Value == other.Value;
    }

    public override string ToString()
    {
        return "MyModelId:" + Value.Value.ToString("x");
    }

    public static bool operator ==(MyModelId left, MyModelId right) => left.Equals(right);

    public static bool operator !=(MyModelId left, MyModelId right) => !left.Equals(right);

    public override bool Equals(object? obj)
    {
        return obj is MyModelId id && Equals(id);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}


public static class MyModelExtensions {
    public static __DI__.IServiceCollection AddMyModelModel(this __DI__.IServiceCollection services) {
        services.AddSingleton<__ABSTRACTIONS__.IAttribute>(_ => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModel.Name);
        return services;
    }


    /// <summary>
    /// Assumes that this model has been commited to the database
    /// in the commit result. Loads this entity from the commited database
    /// and returns a ReadOnly model.
    /// </summary>
    public static MyModel.ReadOnly Remap(this __ABSTRACTIONS__.ICommitResult result, MyModel.New model) {
        return new MyModel.ReadOnly(result.Db, result[model.Id]);
    }

}
