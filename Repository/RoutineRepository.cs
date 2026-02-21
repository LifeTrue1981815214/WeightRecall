using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SQLite;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository
{
    public class RoutineRepository
    {
        private readonly DatabaseContext _context;

        public RoutineRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<RoutineItem>> GetRoutineItemsAsync()
        {
            await _context.InitializeAsync();
            var items = await _context.Connection.Table<RoutineItem>().ToListAsync();
            foreach (var item in items)
            {
                Debug.WriteLine($"[DB DEBUG] ID: {item.Id}, Exercise: {item.ExerciseName}, Day: {item.DayOfWeek}, Order: {item.Order}");
            }

            return items;
        }

        public async Task<List<RoutineItem>> GetRoutineForDayAsync(DayOfWeek day)
        {
            await _context.InitializeAsync();
            var items = await _context.Connection.Table<RoutineItem>()
                .Where(r => r.DayOfWeek == day)
                .OrderBy(r => r.Order)
                .ToListAsync();

                Debug.WriteLine($"[DB DEBUG] Found {items.Count} items for {day}");
            foreach (var item in items)
            {
                Debug.WriteLine($"[DB DEBUG] ID: {item.Id}, Exercise: {item.ExerciseName}, Order: {item.Order}");
            }

            return items;
        }

        public async Task<int> AddRoutineItemAsync(RoutineItem item)
        {
            await _context.InitializeAsync();
            return await _context.Connection.InsertAsync(item);
        }

        public async Task<int> DeleteRoutineItemAsync(RoutineItem Id)
        {
            await _context.InitializeAsync();
            return await _context.Connection.DeleteAsync(Id);
        }
    }
}
