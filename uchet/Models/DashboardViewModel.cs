using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace uchet.Models
{
    public class DashboardViewModel
    {
        // Общее количество имущества
        public int TotalPropertyCount { get; set; }
        
        // Количество имущества, требующего ремонта (с истекшим сроком годности)
        public int ExpiredPropertyCount { get; set; }
        
        // Количество имущества, требующего списания
        public int WriteOffPropertyCount { get; set; }
        
        // Данные для графика расходов по месяцам
        public List<MonthlyExpense> MonthlyExpenses { get; set; } = new List<MonthlyExpense>();
    }
    
    public class MonthlyExpense
    {
        public string Month { get; set; }
        public decimal Expense { get; set; }
    }
}