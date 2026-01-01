namespace BO;

/// <summary>
/// Represents the salary calculation for a courier.
/// </summary>
public class CourierSalary
{
    public int CourierId { get; set; }
    public string CourierName { get; set; } = string.Empty;

    // Base salary components
    public double BaseHourlyRate { get; set; }
    public double HoursWorked { get; set; }
    public double BaseSalary => BaseHourlyRate * HoursWorked;

    // Delivery bonuses
    public int TotalDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int LateDeliveries { get; set; }
    public double PerDeliveryBonus { get; set; }
    public double OnTimeBonusRate { get; set; }
    public double DeliveryBonus => TotalDeliveries * PerDeliveryBonus;
    public double OnTimeBonus => OnTimeDeliveries * OnTimeBonusRate;

    // Distance bonus
    public double TotalDistanceKm { get; set; }
    public double PerKmRate { get; set; }
    public double DistanceBonus => TotalDistanceKm * PerKmRate;

    // Penalties
    public double LatePenaltyRate { get; set; }
    public double LatePenalty => LateDeliveries * LatePenaltyRate;

    // Final calculations
    public double GrossSalary => BaseSalary + DeliveryBonus + OnTimeBonus + DistanceBonus - LatePenalty;
    public double TaxRate { get; set; } = 0.25; // 25% tax
    public double TaxAmount => GrossSalary * TaxRate;
    public double NetSalary => GrossSalary - TaxAmount;

    // Period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public override string ToString()
    {
        return $"""
            ═══════════════════════════════════════════
            💰 SALARY REPORT - {CourierName} (ID: {CourierId})
            Period: {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}
            ═══════════════════════════════════════════
            
            📊 BASE SALARY
               Hourly Rate:        ₪{BaseHourlyRate:F2}
               Hours Worked:       {HoursWorked:F1} hrs
               Base Salary:        ₪{BaseSalary:F2}
            
            🚚 DELIVERY BONUSES
               Total Deliveries:   {TotalDeliveries}
               Per Delivery:       ₪{PerDeliveryBonus:F2} × {TotalDeliveries} = ₪{DeliveryBonus:F2}
               On-Time Deliveries: {OnTimeDeliveries}
               On-Time Bonus:      ₪{OnTimeBonusRate:F2} × {OnTimeDeliveries} = ₪{OnTimeBonus:F2}
            
            📍 DISTANCE BONUS
               Total Distance:     {TotalDistanceKm:F1} km
               Per Km Rate:        ₪{PerKmRate:F2}
               Distance Bonus:     ₪{DistanceBonus:F2}
            
            ⚠️ PENALTIES
               Late Deliveries:    {LateDeliveries}
               Late Penalty:       -₪{LatePenalty:F2}
            
            ───────────────────────────────────────────
            💵 GROSS SALARY:       ₪{GrossSalary:F2}
            📉 Tax ({TaxRate * 100}%):          -₪{TaxAmount:F2}
            ═══════════════════════════════════════════
            💳 NET SALARY:         ₪{NetSalary:F2}
            ═══════════════════════════════════════════
            """;
    }
}
