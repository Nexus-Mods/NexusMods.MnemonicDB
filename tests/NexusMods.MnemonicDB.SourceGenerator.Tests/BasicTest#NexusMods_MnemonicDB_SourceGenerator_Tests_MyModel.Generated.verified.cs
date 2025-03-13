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


/// <summary>
/// The top level model definition for the MyModel model. This class is rarely 
/// used directly, instead, the ReadOnly struct or the New class should be used.
/// </summary>

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
    public static IObservable<DynamicData.IChangeSet<ReadOnly, EntityId>> ObserveAll(IConnection conn)
    {
        return conn.ObserveDatoms(PrimaryAttribute)
            .AsEntityIds()
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


    /// <summary>
    /// Constructs a new MyModel model from the given entity id, used to provide a typed structured
    /// way to interact with the entity before it is commited to the database.
    /// </summary>
    public partial class New : __MODELS__.ITemporaryEntity, __MODELS__.IHasEntityId {

    
    /// <summary>
    /// Constructs a new MyModel model from the given transaction with a generated temporary id.
    /// </summary>
    public New(__ABSTRACTIONS__.ITransaction tx) : base() {
        Id = tx.TempId();
        tx.Attach(this);
    }
    

        /// <summary>
        /// Constructs a new MyModel model from the given transaction with the given entity id.
        /// </summary>
        public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.EntityId eid) : base() {
            Id = eid;
            tx.Attach(this);
        }


        /// <summary>
        /// Adds this model to the given transaction.
        /// </summary>
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
        
        /// <inheritdoc cref="MyModel.Name" />
        public required string Name { get; set; }
        #endregion
    }

    /// <summary>
    /// The ReadOnly struct is a read-only version of the entity, it is used to access the entity
    /// in a read context. It immutable and must be reloaded to get updated data when the entity changes.
    /// </summary>
    
    public readonly partial struct ReadOnly :
        __MODELS__.IReadOnlyModel<MyModel.ReadOnly> {

           /// <summary>
           /// The database segment containing the datoms for this entity in EAVT order.
           /// </summary>
           public readonly __SEGMENTS__.EntitySegment EntitySegment;
           
           __SEGMENTS__.EntitySegment __MODELS__.IHasIdAndEntitySegment.EntitySegment => this.EntitySegment; 

           /// <summary>
           /// Constructs a new ReadOnly model of the entity from the given segment and id.
           /// </summary>
           public ReadOnly(__ABSTRACTIONS__.IDb db, __SEGMENTS__.EntitySegment segment, __ABSTRACTIONS__.EntityId id) {
               Db = db;
               Id = id;
               EntitySegment = segment;
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
           public int Count => EntitySegment.Count;


           /// <inheritdoc />
           public IEnumerator<IReadDatom> GetEnumerator()
           {
               var resolver = Db.Connection.AttributeResolver;
               foreach (var datom in EntitySegment)
               {
                   yield return resolver.Resolve(datom);
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
              => EntitySegment.Contains(attribute);
        

           /// <inheritdoc />
           public override string ToString()
           {
               return "MyModel<" + Id + ">";
           }

           /// <inheritdoc />
           public bool IsValid()
           {
               // This is true when the struct is a default value.
               if (Db == null) return false;

               return this.Contains(PrimaryAttribute);
           }


           /// <inheritdoc cref="MyModel.Name" />
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

           /// <summary>
           /// Implicit conversion from the model to the model id.
           /// </summary>
           public static implicit operator MyModelId(MyModel.ReadOnly? model) {
               return MyModelId.From(model!.Value.Id);
           }
        }
}

/// <summary>
/// A value object representing the id of a MyModel entity.
/// </summary>
[global::System.Text.Json.Serialization.JsonConverter(typeof(MyModelId.JsonConverter))]
public readonly partial struct MyModelId : IEquatable<MyModelId>, IEquatable<__ABSTRACTIONS__.EntityId>
{
    /// <summary>
    /// The generic EntityId value this typed id wraps.
    /// </summary>
    public readonly __ABSTRACTIONS__.EntityId Value;

    /// <summary>
    /// Constructs a new MyModelId from the given entity id.
    /// </summary>
    public MyModelId(__ABSTRACTIONS__.EntityId id) => Value = id;

    /// <summary>
    /// Constructs a new MyModelId from the given entity id.
    /// </summary>
    public static MyModelId From(__ABSTRACTIONS__.EntityId id) => new MyModelId(id);

    /// <summary>
    /// Constructs a new MyModelId from the given ulong.
    /// </summary>
    public static MyModelId From(ulong id) => new MyModelId(__ABSTRACTIONS__.EntityId.From(id));

    /// <summary>
    /// Implicit conversion from the model id to the entity id.
    /// </summary>
    public static implicit operator __ABSTRACTIONS__.EntityId(MyModelId id) => id.Value;
    
    /// <summary>
    /// Implicit conversion from the entity id to the model id.
    /// </summary>
    public static implicit operator MyModelId(EntityId id) => MyModelId.From(id);

    /// <summary>
    /// Equality comparison between two MyModelId values.
    /// </summary>
    public bool Equals(MyModelId other)
    {
        return Value.Value == other.Value.Value;
    }

    /// <summary>
    /// Equality comparison between a MyModelId and an EntityId.
    /// </summary>
    public bool Equals(__ABSTRACTIONS__.EntityId other)
    {
        return Value.Value == other.Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "MyModelId:" + Value.Value.ToString("x");
    }

    /// <inheritdoc />
    public static bool operator ==(MyModelId left, MyModelId right) => left.Equals(right);
    
    /// <inheritdoc />
    public static bool operator !=(MyModelId left, MyModelId right) => !left.Equals(right);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is MyModelId id && Equals(id);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// A JsonConverter for the MyModelId value object.
    /// </summary>
	internal class JsonConverter : global::System.Text.Json.Serialization.JsonConverter<MyModelId>
	{
	    private readonly global::System.Text.Json.Serialization.JsonConverter<__ABSTRACTIONS__.EntityId> _innerConverter = new __ABSTRACTIONS__.EntityId.JsonConverter();

		public override MyModelId Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
		    return _innerConverter.Read(ref reader, typeToConvert, options);
		}

		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, MyModelId value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.Write(writer, value.Value, options);
		}

		public override MyModelId ReadAsPropertyName(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
    		return _innerConverter.ReadAsPropertyName(ref reader, typeToConvert, options);
		}

		public override void WriteAsPropertyName(global::System.Text.Json.Utf8JsonWriter writer, MyModelId value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.WriteAsPropertyName(writer, value.Value, options);
		}
	}
}

/// <summary>
/// Extension methods for the MyModel model.
/// </summary>
public static class MyModelExtensions {

    /// <summary>
    /// Adds the MyModel model to the service collection.
    /// </summary>
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
