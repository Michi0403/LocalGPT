using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
namespace LocalGPT.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static async Task SyncWith<T, TKey>(
            this IList<T> target,
            IEnumerable<T> updated,
            Func<T, TKey> keySelector,
            bool replaceIfDifferent = false, ConfiguredTaskAwaitable? configuredTaskAwaitableToInformUpdates = null)
        where TKey : notnull
        {
            if (target == null || updated == null || keySelector == null)
                return;

            var updatedList = updated.Where(u => u != null).ToList();


            var updatedMap = updatedList
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.First());

            var existingMap = target
                .Where(t => t != null)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.First());


            var toRemove = target
                .Where(item => !updatedMap.ContainsKey(keySelector(item)))
                .ToList();

            foreach (var item in toRemove)
            {
                _ = target.Remove(item);
                if (configuredTaskAwaitableToInformUpdates != null && configuredTaskAwaitableToInformUpdates.HasValue)
                {
                    await configuredTaskAwaitableToInformUpdates.Value;
                }
            }


            foreach (var kvp in updatedMap)
            {
                if (!existingMap.TryGetValue(kvp.Key, out var existing))
                {

                    target.Add(kvp.Value);
                    if (configuredTaskAwaitableToInformUpdates != null && configuredTaskAwaitableToInformUpdates.HasValue)
                    {
                        await configuredTaskAwaitableToInformUpdates.Value;
                    }
                }
                else if (replaceIfDifferent && !Equals(existing, kvp.Value))
                {
                    int index = target.IndexOf(existing);
                    if (index >= 0)
                        target[index] = kvp.Value;
                }
            }
        }
    }










}































