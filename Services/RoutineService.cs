using System;
using System.Collections.Generic;
using System.Text;
using WeightRecall.Data;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services
{
    internal class RoutineService
    {
        private readonly RoutineRepository _repository;

        public RoutineService(RoutineRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoutineItem>> GetRoutineForDay(int day)
        {
            return await _repository.GetRoutineForDayAsync(day);
        }
        static void UpdateRoutine()
        {

        }
        static void ReorderRoutine()
        {

        }

    }
}
