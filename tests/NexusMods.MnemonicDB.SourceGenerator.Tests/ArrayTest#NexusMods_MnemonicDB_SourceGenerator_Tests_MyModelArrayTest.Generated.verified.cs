﻿//HintName: NexusMods_MnemonicDB_SourceGenerator_Tests_MyModelArrayTest.Generated.cs
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



public partial class MyModelArrayTest : __MODELS__.IModelFactory<MyModelArrayTest, MyModelArrayTest.ReadOnly>
{

    

    #region CRUD Methods


    /// <summary>
    /// A list of all required attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] RequiredAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute,
    };

    /// <summary>
    /// The primary attribute of the model, this really just means one of the required attributes of the model
    /// if an entity has this attribute, it is considered a valid entity.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute PrimaryAttribute => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute;

    /// <summary>
    /// A list of all attributes of the model.
    /// </summary>
    public static __ABSTRACTIONS__.IAttribute[] AllAttributes => new __ABSTRACTIONS__.IAttribute[] {
        NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute,
    };

    /// <summary>
    /// Returns all MyModelArrayTest entities in the database.
    /// </summary>
    public static __SEGMENTS__.Entities<MyModelArrayTest.ReadOnly> All(__ABSTRACTIONS__.IDb db) {
        return db.Datoms(PrimaryAttribute).AsModels<MyModelArrayTest.ReadOnly>(db);
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
    public static bool TryGet(__ABSTRACTIONS__.IDb db, __ABSTRACTIONS__.EntityId id, [NotNullWhen(true)] out MyModelArrayTest.ReadOnly? result)
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
    /// Assumes that the ids given point to MyModelArrayTest entities, and
    /// returns a list of ReadOnly models of the entities.
    /// </summary>
    public static IEnumerable<MyModelArrayTest.ReadOnly> Load(__ABSTRACTIONS__.IDb db, IEnumerable<__ABSTRACTIONS__.EntityId> ids) {
        return ids.Select(id => new MyModelArrayTest.ReadOnly(db, id));
    }

    #endregion


    public partial class New : __MODELS__.ITemporaryEntity, __MODELS__.IHasEntityId {


    
    public New(__ABSTRACTIONS__.ITransaction tx) : base() {
        Id = tx.TempId();
        tx.Attach(this);
    }
    

        public New(__ABSTRACTIONS__.ITransaction tx, __ABSTRACTIONS__.EntityId eid) : base() {
            Id = eid;
            tx.Attach(this);
        }


        public void AddTo(__ABSTRACTIONS__.ITransaction tx)
        {
            tx.Add(Id, NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute, MyAttribute, false);


        }

        /// <summary>
        /// Implicit conversion from the model to the entity id.
        /// </summary>
        public static implicit operator __ABSTRACTIONS__.EntityId(MyModelArrayTest.New model) {
            return model.Id;
        }

        /// <summary>
        /// Implicit conversion from the model to the model id.
        /// </summary>
        public static implicit operator MyModelArrayTestId(MyModelArrayTest.New model) {
            return model.MyModelArrayTestId;
        }

        /// <summary>
        /// The entity id of the model as a model id.
        /// </summary>
        public MyModelArrayTestId MyModelArrayTestId => MyModelArrayTestId.From(Id);

        /// <inheritdoc />
        public __ABSTRACTIONS__.EntityId Id { get; private set; }

        #region Attributes
        
        public required int[] MyAttribute { get; set; }
        #endregion
    }

    
    public readonly partial struct ReadOnly :
        __MODELS__.IReadOnlyModel<MyModelArrayTest.ReadOnly> {

           /// <summary>
           /// The database segment containing the datoms for this entity in EAVT order.
           /// </summary>
           public readonly __SEGMENTS__.IndexSegment IndexSegment;
           
           __SEGMENTS__.IndexSegment __MODELS__.IHasIdAndIndexSegment.IndexSegment => this.IndexSegment; 

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
           public MyModelArrayTestId MyModelArrayTestId => MyModelArrayTestId.From(Id);

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
               return "MyModelArrayTest<" + Id + ">";
           }

           public bool IsValid()
           {
               // This is true when the struct is a default value.
               if (Db == null) return false;

               return this.Contains(PrimaryAttribute);
           }


           public int[] MyAttribute => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute.Get(this);





           /// <summary>
           /// Reloads the entity from the given database, essentially
           /// refreshing the entity.
           /// </summary>
           [Pure]
           public ReadOnly Rebase(__ABSTRACTIONS__.IDb db) => new ReadOnly(db, Id);

           /// <summary>
           /// Implicit conversion from the model to the entity id.
           /// </summary>
           public static implicit operator __ABSTRACTIONS__.EntityId(MyModelArrayTest.ReadOnly model) {
               return model.Id;
           }

           public static implicit operator MyModelArrayTestId(MyModelArrayTest.ReadOnly? model) {
               return MyModelArrayTestId.From(model!.Value.Id);
           }
        }
}

/// <summary>
/// A value object representing the id of a MyModelArrayTest entity.
/// </summary>
[global::System.Text.Json.Serialization.JsonConverter(typeof(MyModelArrayTestId.JsonConverter))]
public readonly partial struct MyModelArrayTestId : IEquatable<MyModelArrayTestId>, IEquatable<__ABSTRACTIONS__.EntityId>
{
    public readonly __ABSTRACTIONS__.EntityId Value;

    public MyModelArrayTestId(__ABSTRACTIONS__.EntityId id) => Value = id;

    /// <summary>
    /// Constructs a new MyModelArrayTestId from the given entity id.
    /// </summary>
    public static MyModelArrayTestId From(__ABSTRACTIONS__.EntityId id) => new MyModelArrayTestId(id);

    /// <summary>
    /// Constructs a new MyModelArrayTestId from the given ulong.
    /// </summary>
    public static MyModelArrayTestId From(ulong id) => new MyModelArrayTestId(__ABSTRACTIONS__.EntityId.From(id));

    public static implicit operator __ABSTRACTIONS__.EntityId(MyModelArrayTestId id) => id.Value;
    public static implicit operator MyModelArrayTestId(EntityId id) => MyModelArrayTestId.From(id);


    public bool Equals(MyModelArrayTestId other)
    {
        return Value.Value == other.Value.Value;
    }


    public bool Equals(__ABSTRACTIONS__.EntityId other)
    {
        return Value.Value == other.Value;
    }

    public override string ToString()
    {
        return "MyModelArrayTestId:" + Value.Value.ToString("x");
    }

    public static bool operator ==(MyModelArrayTestId left, MyModelArrayTestId right) => left.Equals(right);

    public static bool operator !=(MyModelArrayTestId left, MyModelArrayTestId right) => !left.Equals(right);

    public override bool Equals(object? obj)
    {
        return obj is MyModelArrayTestId id && Equals(id);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

	public class JsonConverter : global::System.Text.Json.Serialization.JsonConverter<MyModelArrayTestId>
	{
	    private readonly global::System.Text.Json.Serialization.JsonConverter<__ABSTRACTIONS__.EntityId> _innerConverter = new __ABSTRACTIONS__.EntityId.JsonConverter();

		public override MyModelArrayTestId Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
		    return _innerConverter.Read(ref reader, typeToConvert, options);
		}

		public override void Write(global::System.Text.Json.Utf8JsonWriter writer, MyModelArrayTestId value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.Write(writer, value.Value, options);
		}

		public override MyModelArrayTestId ReadAsPropertyName(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
		{
    		return _innerConverter.ReadAsPropertyName(ref reader, typeToConvert, options);
		}

		public override void WriteAsPropertyName(global::System.Text.Json.Utf8JsonWriter writer, MyModelArrayTestId value, global::System.Text.Json.JsonSerializerOptions options)
		{
		    _innerConverter.WriteAsPropertyName(writer, value.Value, options);
		}
	}
}


public static class MyModelArrayTestExtensions {
    public static __DI__.IServiceCollection AddMyModelArrayTestModel(this __DI__.IServiceCollection services) {
        services.AddSingleton<__ABSTRACTIONS__.IAttribute>(_ => NexusMods.MnemonicDB.SourceGenerator.Tests.MyModelArrayTest.MyAttribute);
        return services;
    }


    /// <summary>
    /// Assumes that this model has been commited to the database
    /// in the commit result. Loads this entity from the commited database
    /// and returns a ReadOnly model.
    /// </summary>
    public static MyModelArrayTest.ReadOnly Remap(this __ABSTRACTIONS__.ICommitResult result, MyModelArrayTest.New model) {
        return new MyModelArrayTest.ReadOnly(result.Db, result[model.Id]);
    }

}
