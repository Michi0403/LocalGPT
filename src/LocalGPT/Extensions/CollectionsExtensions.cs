using System.Collections.ObjectModel;
namespace LocalGPT.Extensions
{
    /// <summary>
    /// Represents an observable collection extensions application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Performs sync with for <see cref="ObservableCollectionExtensions"/>, keeping the operation consistent with the state and invariants of the surrounding observable collection extensions workflow.
        /// </summary>
        /// <typeparam name="T">Type used for t values handled by <see cref="ObservableCollectionExtensions"/>.</typeparam>
        /// <typeparam name="TKey">Type used for t key values handled by <see cref="ObservableCollectionExtensions"/>.</typeparam>
        /// <param name="target">T dependency used by the observable collection extensions workflow to provide the corresponding application capability.</param>
        /// <param name="updated">T dependency used by the observable collection extensions workflow to provide the corresponding application capability.</param>
        /// <param name="keySelector">Key selector value supplied to the observable collection extensions operation and used when producing its result.</param>
        /// <param name="replaceIfDifferent">Value indicating whether replace if different should apply to this operation.</param>
        /// <param name="taskToInformUpdates">Optional task awaited after collection mutations so callers can observe each update.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        public static async Task SyncWith<T, TKey>(
            this IList<T> target,
            IEnumerable<T> updated,
            Func<T, TKey> keySelector,
            bool replaceIfDifferent = false, Task? taskToInformUpdates = null)
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
                if (taskToInformUpdates is not null)
                {
                    await taskToInformUpdates.ConfigureAwait(false);
                }
            }


            foreach (var kvp in updatedMap)
            {
                if (!existingMap.TryGetValue(kvp.Key, out var existing))
                {

                    target.Add(kvp.Value);
                    if (taskToInformUpdates is not null)
                    {
                        await taskToInformUpdates.ConfigureAwait(false);
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































