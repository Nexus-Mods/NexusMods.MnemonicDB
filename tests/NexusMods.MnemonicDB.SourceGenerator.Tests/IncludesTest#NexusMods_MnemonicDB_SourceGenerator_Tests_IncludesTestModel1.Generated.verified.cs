//HintName: NexusMods_MnemonicDB_SourceGenerator_Tests_IncludesTestModel1.Generated.cs
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
/// The top level model definition for the IncludesTestModel1 model. This class is rarely 
/// used directly, instead, the ReadOnly struct or the New class should be used.
/// </summary>

public partial class IncludesTestModel1 : __MODELS__.IModelFactory<IncludesTestModel1, IncludesTestModel1.ReadOnly>
{

    

    #region CRUD Methods


    /// <summary>
    /// A list of all required attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] RequiredAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name,
    };

    /// <summary>
    /// The primary attribute of the model, this really just means one of the required attributes of the model
    /// if an entity has this attribute, it is considered a valid entity.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute PrimaryAttribute => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name;

    /// <summary>
    /// A list of all attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] AllAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name,
    };

    /// <summary>
    /// Returns all IncludesTestModel1 entities in the database.
    /// </summary>
    public static __SEGMENTS__.Entities<IncludesTestModel1.ReadOnly> All(__ABSTRACTIONS__.IDb db) {
        return db.Datoms(PrimaryAttribute).AsModels<IncludesTestModel1.ReadOnly>(db);
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
    public static bool TryGet(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id, [NotNullWhen(true)] out IncludesTestModel1.ReadOnly? result)
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
    /// Assumes that the ids given point to IncludesTestModel1 entities, and
    /// returns a list of ReadOnly models of the entities.
    /// </summary>
    public static IEnumerable<IncludesTestModel1.ReadOnly> Load(__ABSTRACTIONS__.IDb db, IEnumerable<__ABSTRACTIONS__.EntityId> ids) {
        return ids.Select(id => new IncludesTestModel1.ReadOnly(db, id));
    }

    #endregion


    /// <summary>
    /// Constructs a new IncludesTestModel1 model from the given entity id, used to provide a typed structured
    /// way to interact with the entity before it is commited to the database.
    /// </summary>
    public partial class New : __MODELS__.ITemporaryEntity, __MODELS__.IHasEntityId {

    
    /// <summary>
    /// Constructs a new IncludesTestModel1 model from the given transaction with a generated temporary id.
    /// </summary>
    public New(__ABSTRACTIONS__.ITransaction tx) : base() {
        Id = tx.TempId();
        tx.Attach(this);
    }
    

        /// <summary>
        /// Constructs a new IncludesTestModel1 model from the given transaction with the given entity id.
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
            tx.Add(Id, NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name, Name, false);


        }

        /// <summary>
        /// Implicit conversion from the model to the entity id.
        /// </summary>
        public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel1.New model) {
            return model.Id;
        }

        /// <summary>
        /// Implicit conversion from the model to the model id.
        /// </summary>
        public static implicit operator IncludesTestModel1Id(IncludesTestModel1.New model) {
            return model.IncludesTestModel1Id;
        }

        /// <summary>
        /// The entity id of the model as a model id.
        /// </summary>
        public IncludesTestModel1Id IncludesTestModel1Id => IncludesTestModel1Id.From(Id);

        /// <inheritdoc />
        public __ABSTRACTIONS__.EntityId Id { get; private set; }

        #region Attributes
        
        /// <inheritdoc cref="IncludesTestModel1.Name" />
        public required string Name { get; set; }
        #endregion
    }

    /// <summary>
    /// The ReadOnly struct is a read-only version of the entity, it is used to access the entity
    /// in a read context. It immutable and must be reloaded to get updated data when the entity changes.
    /// </summary>
    
    public readonly partial struct ReadOnly :
        __MODELS__.IReadOnlyModel<IncludesTestModel1.ReadOnly> {

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
           public IncludesTestModel1Id IncludesTestModel1Id => IncludesTestModel1Id.From(Id);

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
               return "IncludesTestModel1<" + Id + ">";
           }

           /// <inheritdoc />
           public bool IsValid()
           {
               // This is true when the struct is a default value.
               if (Db == null) return false;

               return this.Contains(PrimaryAttribute);
           }


           /// <inheritdoc cref="IncludesTestModel1.Name" />
           public string Name => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name.Get(this);





           /// <summary>
           /// Reloads the entity from the given database, essentially
           /// refreshing the entity.
           /// </summary>
           [Pure]
           public ReadOnly Rebase(__ABSTRACTIONS__.IDb db) => new ReadOnly(db, Id);

           /// <summary>
           /// Implicit conversion from the model to the entity id.
           /// </summary>
           public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel1.ReadOnly model) {
               return model.Id;
           }

           /// <summary>
           /// Implicit conversion from the model to the model id.
           /// </summary>
           public static implicit operator IncludesTestModel1Id(IncludesTestModel1.ReadOnly? model) {
               return IncludesTestModel1Id.From(model!.Value.Id);
           }
        }
}

/// <summary>
/// A value object representing the id of a IncludesTestModel1 entity.
/// </summary>
[global::System.Text.Json.Serialization.JsonConverter(typeof(IncludesTestModel1Id.JsonConverter))]
public readonly partial struct IncludesTestModel1Id : IEquatable<IncludesTestModel1Id>, IEquatable<__ABSTRACTIONS__.EntityId>
{
    /// <summary>
    /// The generic EntityId value this typed id wraps.
    /// </summary>
    public readonly __ABSTRACTIONS__.EntityId Value;

    /// <summary>
    /// Constructs a new IncludesTestModel1Id from the given entity id.
    /// </summary>
    public IncludesTestModel1Id(__ABSTRACTIONS__.EntityId id) => Value = id;

    /// <summary>
    /// Constructs a new IncludesTestModel1Id from the given entity id.
    /// </summary>
    public static IncludesTestModel1Id From(__ABSTRACTIONS__.EntityId id) => new IncludesTestModel1Id(id);

    /// <summary>
    /// Constructs a new IncludesTestModel1Id from the given ulong.
    /// </summary>
    public static IncludesTestModel1Id From(ulong id) => new IncludesTestModel1Id(__ABSTRACTIONS__.EntityId.From(id));

    /// <summary>
    /// Implicit conversion from the model id to the entity id.
    /// </summary>
    public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel1Id id) => id.Value;
    
    /// <summary>
    /// Implicit conversion from the entity id to the model id.
    /// </summary>
    public static implicit operator IncludesTestModel1Id(EntityId id) => IncludesTestModel1Id.From(id);

    /// <summary>
    /// Equality comparison between two IncludesTestModel1Id values.
    /// </summary>
    public bool Equals(IncludesTestModel1Id other)
    {
        return Value.Value == other.Value.Value;
    }

    /// <summary>
    /// Equality comparison between a IncludesTestModel1Id and an EntityId.
    /// </summary>
    public bool Equals(__ABSTRACTIONS__.EntityId other)
    {
        return Value.Value == other.Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "IncludesTestModel1Id:" + Value.Value.ToString("x");
    }

    /// <inheritdoc />
    public static bool operator ==(IncludesTestModel1Id left, IncludesTestModel1Id right) => left.Equals(right);
    
    /// <inheritdoc />
    public static bool operator !=(IncludesTestModel1Id left, IncludesTestModel1Id right) => !left.Equals(right);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is IncludesTestModel1Id id && Equals(id);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// A JsonConverter for the IncludesTestModel1Id value object.
    /// </summary>
	internal class JsonConverter : global::System.Text.Json.Serialization.JsonConverter<IncludesTestModel1Id>
	{
	    private readonly global::System.Text.Json.Serialization.JsonConverter<__ABSTRACTIONS__.EntityId> _innerConverter = new __ABSTRACTIONS__.EntityId.JsonConverter();

		public override IncludesTestModel1Id Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
		    return _innerConverter.Read(ref reader, typeToConvert, options);
		}

		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, IncludesTestModel1Id value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.Write(writer, value.Value, options);
		}

		public override IncludesTestModel1Id ReadAsPropertyName(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
    		return _innerConverter.ReadAsPropertyName(ref reader, typeToConvert, options);
		}

		public override void WriteAsPropertyName(global::System.Text.Json.Utf8JsonWriter writer, IncludesTestModel1Id value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.WriteAsPropertyName(writer, value.Value, options);
		}
	}
}

/// <summary>
/// Extension methods for the IncludesTestModel1 model.
/// </summary>
public static class IncludesTestModel1Extensions {

    /// <summary>
    /// Adds the IncludesTestModel1 model to the service collection.
    /// </summary>
    public static __DI__.IServiceCollection AddIncludesTestModel1Model(this __DI__.IServiceCollection services) {
        services.AddSingleton<__ABSTRACTIONS__.IAttribute>(_ => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name);
        return services;
    }


    /// <summary>
    /// Assumes that this model has been commited to the database
    /// in the commit result. Loads this entity from the commited database
    /// and returns a ReadOnly model.
    /// </summary>
    public static IncludesTestModel1.ReadOnly Remap(this __ABSTRACTIONS__.ICommitResult result, IncludesTestModel1.New model) {
        return new IncludesTestModel1.ReadOnly(result.Db, result[model.Id]);
    }

}
