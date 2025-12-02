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
        
        // Общая стоимость всего имущества
        public decimal TotalPropertyCost { get; set; }
        
        // Количество имущества, требующего ремонта (с истекшим сроком годности)
        public int ExpiredPropertyCount { get; set; }
        
        // Стоимость имущества, требующего ремонта (с истекшим сроком годности)
        public decimal ExpiredPropertyCost { get; set; }
        
        // Количество имущества, требующего списания
        public int WriteOffPropertyCount { get; set; }
        
        // Данные для графика поступлений имущества по месяцам
        public List<MonthlyAcquisition> MonthlyAcquisitions { get; set; } = new List<MonthlyAcquisition>();
        
        // Данные для круговой диаграммы проверенного/непроверенного имущества
        public int CheckedPropertyCount { get; set; }
        public int UncheckedPropertyCount { get; set; }
    }
    
    public class MonthlyAcquisition
    {
        public string Month { get; set; }
        public int Count { get; set; }
        public decimal TotalCost { get; set; }
    }
}