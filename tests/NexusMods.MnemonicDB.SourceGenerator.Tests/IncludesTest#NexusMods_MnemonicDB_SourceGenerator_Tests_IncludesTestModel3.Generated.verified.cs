//HintName: NexusMods_MnemonicDB_SourceGenerator_Tests_IncludesTestModel3.Generated.cs
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
/// The top level model definition for the IncludesTestModel3 model. This class is rarely 
/// used directly, instead, the ReadOnly struct or the New class should be used.
/// </summary>

public partial class IncludesTestModel3 : __MODELS__.IModelFactory<IncludesTestModel3, IncludesTestModel3.ReadOnly>
{

    

    #region CRUD Methods


    /// <summary>
    /// A list of all required attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] RequiredAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name,
    };

    /// <summary>
    /// The primary attribute of the model, this really just means one of the required attributes of the model
    /// if an entity has this attribute, it is considered a valid entity.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute PrimaryAttribute => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name;

    /// <summary>
    /// A list of all attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] AllAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name,
    };

    /// <summary>
    /// Returns all IncludesTestModel3 entities in the database.
    /// </summary>
    public static __SEGMENTS__.Entities<IncludesTestModel3.ReadOnly> All(__ABSTRACTIONS__.IDb db) {
        return db.Datoms(PrimaryAttribute).AsModels<IncludesTestModel3.ReadOnly>(db);
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
    public static bool TryGet(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id, [NotNullWhen(true)] out IncludesTestModel3.ReadOnly? result)
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
    /// Assumes that the ids given point to IncludesTestModel3 entities, and
    /// returns a list of ReadOnly models of the entities.
    /// </summary>
    public static IEnumerable<IncludesTestModel3.ReadOnly> Load(__ABSTRACTIONS__.IDb db, IEnumerable<__ABSTRACTIONS__.EntityId> ids) {
        return ids.Select(id => new IncludesTestModel3.ReadOnly(db, id));
    }

    #endregion


    /// <summary>
    /// Constructs a new IncludesTestModel3 model from the given entity id, used to provide a typed structured
    /// way to interact with the entity before it is commited to the database.
    /// </summary>
    public partial class New : __MODELS__.ITemporaryEntity, __MODELS__.IHasEntityId {

    
    /// <summary>
    /// Constructs a new IncludesTestModel3 model from the given transaction with the given entity id.
    /// </summary>
    public New(__ABSTRACTIONS__.ITransaction tx, out __ABSTRACTIONS__.EntityId id) : base() {
        Id = tx.TempId();
        id = Id;
        tx.Attach(this);
    }
    
    /// <summary>
    /// Constructs a new IncludesTestModel3 model from the given transaction with the given entity partition for the generated temporary id.
    /// </summary>
    public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.PartitionId partition, out __ABSTRACTIONS__.EntityId id) : base() {
        Id = tx.TempId(partition);
        id = Id;
        tx.Attach(this);
    }
    

        /// <summary>
        /// Constructs a new IncludesTestModel3 model from the given transaction with the given entity id.
        /// </summary>
        public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.EntityId eid) : base() {
            Id = eid;
            tx.Attach(this);
        }

    /// <summary>
    /// Gets the included entity of type IncludesTestModel2 from the transaction.
    /// </summary>
    public NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.New GetIncludesTestModel2(__ABSTRACTIONS__.ITransaction tx) {
        if (!tx.TryGet<NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.New>(Id, out var value))
            throw new InvalidOperationException($"Unable to find {typeof(NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.New)} with ID {Id} in transaction!");

        return value;
    }

    /// <summary>
    /// Sets the included entity of type IncludesTestModel2 in the transaction.
    /// </summary>
    public required NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.New IncludesTestModel2 {
        init {
            if (Id != value.Id)
                throw new InvalidOperationException("The id of the included entity must match the id of the including entity.");
        }
    }

        /// <summary>
        /// Adds this model to the given transaction.
        /// </summary>
        public void AddTo(__ABSTRACTIONS__.ITransaction tx)
        {
            tx.Add(Id, NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name, Name, false);


        }

        /// <summary>
        /// Implicit conversion from the model to the entity id.
        /// </summary>
        public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel3.New model) {
            return model.Id;
        }

        /// <summary>
        /// Implicit conversion from the model to the model id.
        /// </summary>
        public static implicit operator IncludesTestModel3Id(IncludesTestModel3.New model) {
            return model.IncludesTestModel3Id;
        }

        /// <summary>
        /// The entity id of the model as a model id.
        /// </summary>
        public IncludesTestModel3Id IncludesTestModel3Id => IncludesTestModel3Id.From(Id);

        /// <inheritdoc />
        public __ABSTRACTIONS__.EntityId Id { get; private set; }

        #region Attributes
        
        /// <inheritdoc cref="IncludesTestModel3.Name" />
        public required string Name { get; set; }
        #endregion
    }

    /// <summary>
    /// The ReadOnly struct is a read-only version of the entity, it is used to access the entity
    /// in a read context. It immutable and must be reloaded to get updated data when the entity changes.
    /// </summary>
    
    public readonly partial struct ReadOnly :
        __MODELS__.IReadOnlyModel<IncludesTestModel3.ReadOnly> {

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
           public IncludesTestModel3Id IncludesTestModel3Id => IncludesTestModel3Id.From(Id);

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
               return "IncludesTestModel3<" + Id + ">";
           }

           /// <inheritdoc />
           public bool IsValid()
           {
               // This is true when the struct is a default value.
               if (Db == null) return false;

               return this.Contains(PrimaryAttribute);
           }

           /// <summary>
           /// Tries convert this entity to a IncludesTestModel2 entity, if the entity is not a IncludesTestModel2 entity, it returns false.
           /// </summary>
           public NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly AsIncludesTestModel2() => new NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly(Db, Id);

           /// <inheritdoc cref="IncludesTestModel3.Name" />
           public string Name => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name.Get(this);



           /// <inheritdoc cref="IncludesTestModel2.Name" />
           public string IncludesTestModel2_Name => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.Name.Get(this);



           /// <inheritdoc cref="IncludesTestModel1.Name" />
           public string IncludesTestModel1_Name => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel1.Name.Get(this);





           /// <summary>
           /// Reloads the entity from the given database, essentially
           /// refreshing the entity.
           /// </summary>
           [Pure]
           public ReadOnly Rebase(__ABSTRACTIONS__.IDb db) => new ReadOnly(db, Id);

           /// <summary>
           /// Implicit conversion from the model to the entity id.
           /// </summary>
           public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel3.ReadOnly model) {
               return model.Id;
           }

           /// <summary>
           /// Implicit conversion from the model to the model id.
           /// </summary>
           public static implicit operator IncludesTestModel3Id(IncludesTestModel3.ReadOnly? model) {
               return IncludesTestModel3Id.From(model!.Value.Id);
           }
        }
}

/// <summary>
/// A value object representing the id of a IncludesTestModel3 entity.
/// </summary>
[global::System.Text.Json.Serialization.JsonConverter(typeof(IncludesTestModel3Id.JsonConverter))]
public readonly partial struct IncludesTestModel3Id : IEquatable<IncludesTestModel3Id>, IEquatable<__ABSTRACTIONS__.EntityId>
{
    /// <summary>
    /// The generic EntityId value this typed id wraps.
    /// </summary>
    public readonly __ABSTRACTIONS__.EntityId Value;

    /// <summary>
    /// Constructs a new IncludesTestModel3Id from the given entity id.
    /// </summary>
    public IncludesTestModel3Id(__ABSTRACTIONS__.EntityId id) => Value = id;

    /// <summary>
    /// Constructs a new IncludesTestModel3Id from the given entity id.
    /// </summary>
    public static IncludesTestModel3Id From(__ABSTRACTIONS__.EntityId id) => new IncludesTestModel3Id(id);

    /// <summary>
    /// Constructs a new IncludesTestModel3Id from the given ulong.
    /// </summary>
    public static IncludesTestModel3Id From(ulong id) => new IncludesTestModel3Id(__ABSTRACTIONS__.EntityId.From(id));

    /// <summary>
    /// Implicit conversion from the model id to the entity id.
    /// </summary>
    public static implicit operator __ABSTRACTIONS__.EntityId(IncludesTestModel3Id id) => id.Value;
    
    /// <summary>
    /// Implicit conversion from the entity id to the model id.
    /// </summary>
    public static implicit operator IncludesTestModel3Id(EntityId id) => IncludesTestModel3Id.From(id);

    /// <summary>
    /// Equality comparison between two IncludesTestModel3Id values.
    /// </summary>
    public bool Equals(IncludesTestModel3Id other)
    {
        return Value.Value == other.Value.Value;
    }

    /// <summary>
    /// Equality comparison between a IncludesTestModel3Id and an EntityId.
    /// </summary>
    public bool Equals(__ABSTRACTIONS__.EntityId other)
    {
        return Value.Value == other.Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "IncludesTestModel3Id:" + Value.Value.ToString("x");
    }

    /// <inheritdoc />
    public static bool operator ==(IncludesTestModel3Id left, IncludesTestModel3Id right) => left.Equals(right);
    
    /// <inheritdoc />
    public static bool operator !=(IncludesTestModel3Id left, IncludesTestModel3Id right) => !left.Equals(right);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is IncludesTestModel3Id id && Equals(id);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// A JsonConverter for the IncludesTestModel3Id value object.
    /// </summary>
	internal class JsonConverter : global::System.Text.Json.Serialization.JsonConverter<IncludesTestModel3Id>
	{
	    private readonly global::System.Text.Json.Serialization.JsonConverter<__ABSTRACTIONS__.EntityId> _innerConverter = new __ABSTRACTIONS__.EntityId.JsonConverter();

		public override IncludesTestModel3Id Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
		    return _innerConverter.Read(ref reader, typeToConvert, options);
		}

		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, IncludesTestModel3Id value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.Write(writer, value.Value, options);
		}

		public override IncludesTestModel3Id ReadAsPropertyName(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
    		return _innerConverter.ReadAsPropertyName(ref reader, typeToConvert, options);
		}

		public override void WriteAsPropertyName(global::System.Text.Json.Utf8JsonWriter writer, IncludesTestModel3Id value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.WriteAsPropertyName(writer, value.Value, options);
		}
	}
}

/// <summary>
/// Extension methods for the IncludesTestModel3 model.
/// </summary>
public static class IncludesTestModel3Extensions {

    /// <summary>
    /// Adds the IncludesTestModel3 model to the service collection.
    /// </summary>
    public static __DI__.IServiceCollection AddIncludesTestModel3Model(this __DI__.IServiceCollection services) {
        services.AddSingleton<__ABSTRACTIONS__.IAttribute>(_ => NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel3.Name);
        return services;
    }

/// <summary>
/// Tries to get the entity as a IncludesTestModel3 entity, if the entity is not a IncludesTestModel3 entity, it returns false.
/// </summary>
public static bool TryGetAsIncludesTestModel3(this NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly model, [NotNullWhen(true)] out IncludesTestModel3.ReadOnly result) {
    var casted = new IncludesTestModel3.ReadOnly(model.Db, model.EntitySegment, model.Id);
    if (casted.IsValid()) {
        result = casted;
        return true;
    }

    result = default;
    return false;
}

/// <summary>
/// Gets entities from the given collection that are of type IncludesTestModel3
/// </summary>
public static IEnumerable<IncludesTestModel3.ReadOnly> OfTypeIncludesTestModel3(this IEnumerable<NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly> models) {
    foreach (var model in models)
    {
        if (model.TryGetAsIncludesTestModel3(out var casted))
        {
            yield return casted;
        }
    }
}

/// <summary>
/// Gets entities from the given collection that are of type IncludesTestModel3
/// </summary>
public static IncludesTestModel3.ReadOnly ToIncludesTestModel3(this NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly model) {
    if (!model.TryGetAsIncludesTestModel3(out var casted))
    {
        throw new InvalidCastException("The model is not a IncludesTestModel3 entity.");
    }

    return casted;
}

/// <summary>
/// Returns true if the entity is a IncludesTestModel3 entity.
/// </summary>
public static bool IsIncludesTestModel3(this NexusMods.MnemonicDB.SourceGenerator.Tests.IncludesTestModel2.ReadOnly model) {
    return model.TryGetAsIncludesTestModel3(out var _);
}

    /// <summary>
    /// Assumes that this model has been commited to the database
    /// in the commit result. Loads this entity from the commited database
    /// and returns a ReadOnly model.
    /// </summary>
    public static IncludesTestModel3.ReadOnly Remap(this __ABSTRACTIONS__.ICommitResult result, IncludesTestModel3.New model) {
        return new IncludesTestModel3.ReadOnly(result.Db, result[model.Id]);
    }

}
