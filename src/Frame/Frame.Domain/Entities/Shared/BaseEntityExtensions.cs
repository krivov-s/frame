using Frame.Domain.Entities.Core;
using Frame.Shared;

namespace Frame.Domain.Entities.Shared;

public static class BaseEntityExtensions
{
    public static Result IsBaseEntity(this object data)
    {
        return data is not BaseEntity entity 
            ? Result<BaseEntity>.Error("Объект не является BaseEntity") 
            : Result<BaseEntity>.Success(entity);
    }
    
    // /// <summary>
    // /// Копирование объекта со всеми вложенными подобъектами
    // /// </summary>
    // /// <param name="sourceObject"></param>
    // /// <typeparam name="TEntity"></typeparam>
    // /// <returns></returns>
    // public static Result<TEntity> DeepCopy<TEntity>(this TEntity sourceObject, object? ownerObj = null) where TEntity: class
    // {
    //     try
    //     {
    //         var type = sourceObject.GetType();
    //         var properties = type.GetProperties();
    //
    //         TEntity? clonedObj = (TEntity?)Activator.CreateInstance(type);
    //         if (clonedObj == null)
    //         {
    //             return Result<TEntity>.Error($"Не удалось создать новый экземпляр класса {type.Name}");
    //         }
    //
    //         foreach (var property in properties)
    //         {
    //             if (property.CanWrite)
    //             {
    //                 object? value = property.GetValue(sourceObject);
    //                 if (value != null && value.GetType().IsClass)
    //                 {
    //                     // Значение ссылочного типа: копируем класс
    //                     string? fillName = value.GetType().FullName;
    //                     if (fillName != null && !fillName.StartsWith("System."))
    //                     {
    //                         // Проверим, а вдруг копируемый атрибут равен ownerObj (это ссылка на владельца)
    //                         // Если так - его не нужно клонировать, иначе это будет бесконечная рекурсия.
    //                         if (value == ownerObj)
    //                         {
    //                             property.SetValue(clonedObj, value);
    //                         }
    //                         else
    //                         {
    //                             Result<object> resObj = DeepCopy(value, sourceObject);
    //                             if (resObj.IsError || resObj.Value == null)
    //                             {
    //                                 return Result<TEntity>.Error(resObj.ErrorResult);
    //                             }
    //                             // Если только что склонированный объект является BaseEntity - зануляем у него Id
    //                             object newObject = resObj.Value;
    //                             BaseEntity? entity = newObject as BaseEntity;
    //                             if (entity != null)
    //                             {
    //                                 entity.Id = 0;
    //                             }
    //                             property.SetValue(clonedObj, newObject);
    //                         }
    //                     }
    //                 }
    //                 else
    //                 {
    //                     // Значение скалярного типа: просто копируем значение 
    //                     property.SetValue(clonedObj, value);
    //                 }
    //             }
    //         }
    //
    //         return clonedObj;
    //     }
    //     catch (Exception ex)
    //     {
    //         return Result<TEntity>.Error($"Ошибка копирования {sourceObject}: {ex.Message}", ex);
    //     }
    // }
}
