using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
 /// A scalar attribute that can be exposed on an entity.
 /// </summary>
 public class ScalarAttribute<TOwner, TType>(string attrName) : IAttribute<ScalarAccumulator<TType>>
 where TOwner : AEntity<TOwner>
 {
     /// <inheritdoc />
     public Type Owner => typeof(TOwner);

     /// <inheritdoc />
     public string Name => attrName;

     /// <inheritdoc />
     public ScalarAccumulator<TType> CreateAccumulator()
     {
         return new ScalarAccumulator<TType>();
     }

     /// <summary>
     /// Sets the value of the attribute for the given entity.
     /// </summary>
     /// <param name="context"></param>
     /// <param name="owner"></param>
     /// <param name="value"></param>
     /// <typeparam name="TContext"></typeparam>
     public void Set<TContext>(TContext context, EntityId<TOwner> owner, TType value)
         where TContext : IEventContext
     {
         if (context.GetAccumulator<TOwner, ScalarAttribute<TOwner, TType>, ScalarAccumulator<TType>>(owner, this, out var accumulator))
             accumulator.Value = value;
         EntityStructureRegistry.Register(this);
     }

     /// <summary>
     /// Resets the value of the attribute for the given entity to the default value.
     /// </summary>
     /// <param name="context"></param>
     /// <param name="owner"></param>
     /// <typeparam name="TContext"></typeparam>
     public void Unset<TContext>(TContext context, EntityId<TOwner> owner)
         where TContext : IEventContext
     {
         if (context.GetAccumulator<TOwner, ScalarAttribute<TOwner, TType>, ScalarAccumulator<TType>>(owner, this, out var accumulator))
             accumulator.Value = default!;
     }

     /// <summary>
     /// Gets the value of the attribute for the given entity.
     /// </summary>
     /// <param name="context"></param>
     /// <param name="owner"></param>
     /// <typeparam name="TContext"></typeparam>
     /// <returns></returns>
     public TType Get(TOwner owner)
     {
         if (owner.Context.GetReadOnlyAccumulator<TOwner, ScalarAttribute<TOwner, TType>, ScalarAccumulator<TType>>(owner.Id, this, out var accumulator))
             return accumulator.Value;
         // TODO, make this a custom exception and extract it to another method
         throw new InvalidOperationException($"Attribute not found for {Name} on {Owner.Name} with id {owner.Id}");
     }
 }

 /// <summary>
 /// A scalar attribute accumulator, used to store a single value
 /// </summary>
 /// <typeparam name="TVal"></typeparam>
 public class ScalarAccumulator<TVal> : IAccumulator
 {
     /// <summary>
     /// The value of the accumulator
     /// </summary>
     public TVal Value = default! ;
 }
