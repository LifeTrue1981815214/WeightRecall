using System;
using System.Collections.Generic;
using System.Text;
using WeightRecall.Data;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services
{
    public class RoutineService
    {
        private readonly RoutineRepository _repository;

        public RoutineService(RoutineRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoutineItem>> GetRoutineForDay(DayOfWeek day)
        {
            return await _repository.GetRoutineForDayAsync(day);
        }
        public async Task<int> DeleteRoutineItem(RoutineItem item)
        {
            return await _repository.DeleteRoutineItemAsync(item);
        }
        public async Task<int> AddRoutineItem(RoutineItem item)
        {
            return await _repository.AddRoutineItemAsync(item);
        }

    }
}
