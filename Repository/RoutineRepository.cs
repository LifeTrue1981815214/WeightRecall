using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository
{
    public class RoutineRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public RoutineRepository(DatabaseContext context)
        {
            _database = context.Connection;
        }

        public async Task<List<RoutineItem>> GetRoutineItemsAsync()
        {
            return await _database.Table<RoutineItem>().ToListAsync();
        }

        public async Task<List<RoutineItem>> GetRoutineForDayAsync(int dayOfWeek)
        {
            return await _database.Table<RoutineItem>()
                                  .Where(i => i.DayOfWeek == dayOfWeek)
                                  .OrderBy(i => i.Order)
                                  .ToListAsync();
        }

        public async Task<int> SaveRoutineItemAsync(RoutineItem item)
        {
            if (item.Id != 0)
                return await _database.UpdateAsync(item);
            else
                return await _database.InsertAsync(item);
        }

        public async Task<int> DeleteRoutineItemAsync(RoutineItem item)
        {
            return await _database.DeleteAsync(item);
        }
    }
}
