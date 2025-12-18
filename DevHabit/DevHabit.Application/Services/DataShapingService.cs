using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using DevHabit.Contracts.Habits;

namespace DevHabit.Application.Services;

public class DataShapingService
{

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    public ExpandoObject ShapedData<T>(T entity, string? fields)
    {
        HashSet<string> fieldsSet = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        propertyInfos = propertyInfos
            .Where(p => fieldsSet.Contains(p.Name))
            .ToArray();

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos.Where(x => fieldsSet.Contains(x.Name))
                .ToArray();
        }

        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            if (fieldsSet.Count == 0 || fieldsSet.Contains(propertyInfo.Name))
            {
                shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
            }
        }

        return (ExpandoObject)shapedObject;
    }

    public List<ExpandoObject> ShapeCollectionData<T>(IEnumerable<T> entities, string? fields, Func<T,List<Link>>? linkFactory)
    {
        HashSet<string> fieldsSet = fields?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        propertyInfos = propertyInfos
            .Where(p => fieldsSet.Contains(p.Name))
            .ToArray();

        List<ExpandoObject> shapedDataList = new();

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos.Where(x => fieldsSet.Contains(x.Name))
                .ToArray();
        }

        foreach (T entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (fieldsSet.Count == 0 || fieldsSet.Contains(propertyInfo.Name))
                {
                    shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
                }

                if (linkFactory is not null)
                {
                    shapedObject["links"] = linkFactory(entity);
                }
            }

            shapedDataList.Add((ExpandoObject)shapedObject);
        }

        return shapedDataList;
    }

    public bool Validate<T>(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }

        var fieldsSet = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        PropertyInfo[] propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return fieldsSet.All(f => propertyInfos.Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)));
    }
}
