
//namespace Frame.Domain.Entities.Core
//{
    // public class BaseGenericEntity<TEntity> : BaseEntity where TEntity : BaseEntity
    // {
    //     public virtual List<Field<TEntity>> GetFields()
    //     {
    //         return [];
    //     }
    //
    //     public Result<TEntity> Clone()
    //     {
    //         // TODO: доразбираться с клонированием потом, когда будет время.
    //         throw new NotImplementedException();
    //         // try
    //         // {
    //         //     var type = GetType();
    //         //     BaseGenericEntity<TEntity>? targetBaseEntity = (BaseGenericEntity<TEntity>?)Activator.CreateInstance(type);
    //         //     if (targetBaseEntity == null)
    //         //     {
    //         //         return Result<TEntity>.Error($"Не удалось создать новый экземпляр класса {type.Name}");
    //         //     }
    //         //
    //         //     TEntity? targetEntity = targetBaseEntity as TEntity;
    //         //     if (targetEntity == null)
    //         //     {
    //         //         return Result<TEntity>.Error($"Созданный экземпляр не является {typeof(TEntity).Name}");
    //         //     }
    //         //
    //         //     TEntity? sourceEntity = this as TEntity;
    //         //     if (sourceEntity == null)
    //         //     {
    //         //         return Result<TEntity>.Error($"Хрень! Существующий экземпляр (источник) не является {typeof(TEntity).Name}");
    //         //     }
    //         //
    //         //     // Цикл по всем объявленным полям объекта.
    //         //     // Правила копирования ассоциаций прописаны в атрибуте RefField.CloneBehavour
    //         //     List<Field<TEntity>> fields = targetBaseEntity.GetFields();
    //         //     foreach (var field in fields)
    //         //     {
    //         //         if (field.IsPersistent)
    //         //         {
    //         //             field.CopyValue(sourceEntity, targetEntity);
    //         //         }
    //         //     }
    //         //     
    //         //     // У клона зануляем Id, он еще не в БД
    //         //     targetBaseEntity.Id = 0;
    //         //     return DoDeepCopy(targetEntity);
    //         //
    //         //     // foreach (var property in properties)
    //         //     // {
    //         //     //     if (property.CanWrite)
    //         //     //     {
    //         //     //         object? value = property.GetValue(this);
    //         //     //         if (value != null && value.GetType().IsClass)
    //         //     //         {
    //         //     //             // Значение ссылочного типа
    //         //     //             if (value.GetType().Name.StartsWith("List"))
    //         //     //             {
    //         //     //                 continue;
    //         //     //             }
    //         //     //             
    //         //     //             // Объекты, наследованные от BaseEntity пропускаем.
    //         //     //             BaseEntity? entity = value as BaseEntity;
    //         //     //             if (entity != null)
    //         //     //             {
    //         //     //                 continue;
    //         //     //             }
    //         //     //             property.SetValue(entity, value);
    //         //     //             // string? fillName = value.GetType().FullName;
    //         //     //             // if (fillName != null && !fillName.StartsWith("System."))
    //         //     //             // {
    //         //     //             // }
    //         //     //         }
    //         //     //         else
    //         //     //         {
    //         //     //             // Значение скалярного типа: просто копируем значение 
    //         //     //             property.SetValue(entity, value);
    //         //     //         }
    //         //     //     }
    //         //     // }
    //         //     //
    //         // }
    //         // catch (Exception ex)
    //         // {
    //         //     return Result<TEntity>.Error($"Ошибка копирования {this}: {ex.Message}", ex);
    //         // }
    //     }
    //     
    //     /// <summary>
    //     /// По-умолчанию метод создает поверхностную (shallow) копию объекта.
    //     /// Для создания полной (deep) копии необходимо в объекте переопределить метод
    //     /// <see cref="DoDeepCopy"/>
    //     /// </summary>
    //     /// <returns></returns>
    //     /// <exception cref="Exception"></exception>
    //     // public TEntity Clone()
    //     // {
    //     //     object newObject = MemberwiseClone();
    //     //     TEntity? newEntity = newObject as TEntity;
    //     //     if (newEntity == null)
    //     //     {
    //     //         throw new Exception($"Поверхностное клонирование объекта {this} вернуло null");
    //     //     }
    //     //     // У клона зануляем Id, он еще не в БД
    //     //     newEntity.Id = 0;
    //     //     return DoDeepCopy(newEntity);
    //     // }
    //     
    //     // public Result<TEntity> Clone()
    //     // {
    //     //     try
    //     //     {
    //     //         var type = GetType();
    //     //         var properties = type.GetProperties();
    //     //
    //     //         TEntity? newEntity = (TEntity?)Activator.CreateInstance(type);
    //     //         if (newEntity == null)
    //     //         {
    //     //             return Result<TEntity>.Error($"Не удалось создать новый экземпляр класса {type.Name}");
    //     //         }
    //     //
    //     //         foreach (var property in properties)
    //     //         {
    //     //             if (property.CanWrite)
    //     //             {
    //     //                 object? value = property.GetValue(this);
    //     //                 if (value != null && value.GetType().IsClass)
    //     //                 {
    //     //                     // Значение ссылочного типа
    //     //                     if (value.GetType().Name.StartsWith("List"))
    //     //                     {
    //     //                         continue;
    //     //                     }
    //     //                     
    //     //                     // Объекты, наследованные от BaseEntity пропускаем.
    //     //                     BaseEntity? entity = value as BaseEntity;
    //     //                     if (entity != null)
    //     //                     {
    //     //                         continue;
    //     //                     }
    //     //                     property.SetValue(newEntity, value);
    //     //                     // string? fillName = value.GetType().FullName;
    //     //                     // if (fillName != null && !fillName.StartsWith("System."))
    //     //                     // {
    //     //                     // }
    //     //                 }
    //     //                 else
    //     //                 {
    //     //                     // Значение скалярного типа: просто копируем значение 
    //     //                     property.SetValue(newEntity, value);
    //     //                 }
    //     //             }
    //     //         }
    //     //
    //     //         // У клона зануляем Id, он еще не в БД
    //     //         newEntity.Id = 0;
    //     //         return DoDeepCopy(newEntity);
    //     //     }
    //     //     catch (Exception ex)
    //     //     {
    //     //         return Result<TEntity>.Error($"Ошибка копирования {this}: {ex.Message}", ex);
    //     //     }
    //     // }
    //     
    //     /// <summary>
    //     /// Метод, принимающий на вход копию объекта, в которую были скопированы все атрибуты кроме связанных списков.
    //     /// Объекты-ссылки копировались на основании метаинформации в поле <see cref="RefField"/> 
    //     /// Задача метода - докопировать списки, скорректировать (при необходимости) ассоциации,
    //     /// чтобы сделать полную новую копию объекта (deep copy). 
    //     /// </summary>
    //     /// <param name="newEntity">Только что созданная копия объекта с заполненными линейными атрибутами (shallow copy)</param>
    //     /// <returns></returns>
    //     protected virtual Result<TEntity> DoDeepCopy(TEntity newEntity)
    //     {
    //         return Result<TEntity>.Success(newEntity);
    //     }
    // }
//}
