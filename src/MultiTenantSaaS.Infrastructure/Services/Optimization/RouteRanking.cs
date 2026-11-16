using MultiTenantSaaS.Application.DTOs.Optimization;

namespace MultiTenantSaaS.Infrastructure.Services.Optimization;

/// <summary>Pareto ranking and min-max normalised weighted scoring across the feasible scenario results of one run.</summary>
internal static class RouteRanking
{
    private static readonly (string Name, Func<OptimizationScenarioResultDto, double> Value, Func<RankingWeightsDto, double> Weight)[] Objectives =
    {
        ("time", r => r.TimeToGoHours, w => w.Time),
        ("fuel", r => r.Analysis.TotalFuelMt, w => w.Fuel),
        ("cost", r => r.TotalCostToGoUsd, w => w.Cost),
        ("emissions", r => r.MeCo2EmissionsToGoMt, w => w.Emissions),
        ("weather exposure", r => r.Analysis.WeatherExposureScore, w => w.Weather),
    };

    public static void Apply(List<OptimizationScenarioResultDto> results, RankingWeightsDto? weights)
    {
        weights ??= new RankingWeightsDto();
        var feasible = results.Where(result => result.IsFeasible).ToList();
        if (feasible.Count == 0) return;

        var remaining = feasible.ToList();
        var rank = 1;
        while (remaining.Count > 0)
        {
            var front = remaining.Where(candidate => !remaining.Any(other => other != candidate && Dominates(other, candidate))).ToList();
            foreach (var result in front)
            {
                result.Analysis.ParetoRank = rank;
                result.Analysis.IsParetoOptimal = rank == 1;
            }
            remaining = remaining.Except(front).ToList();
            rank++;
        }

        var totalWeight = Objectives.Sum(objective => Math.Max(0, objective.Weight(weights)));
        var ranges = Objectives.Select(objective =>
        {
            var values = feasible.Select(objective.Value).ToList();
            return (Min: values.Min(), Max: values.Max());
        }).ToArray();

        foreach (var result in feasible)
        {
            var score = 0.0;
            for (var index = 0; index < Objectives.Length; index++)
            {
                var weight = Math.Max(0, Objectives[index].Weight(weights));
                var span = ranges[index].Max - ranges[index].Min;
                var normalised = span < 1e-9 ? 0 : (Objectives[index].Value(result) - ranges[index].Min) / span;
                score += totalWeight > 0 ? weight / totalWeight * normalised : 0;
            }
            result.Analysis.RankingScore = score;
        }

        var ordered = feasible.OrderBy(result => result.Analysis.RankingScore).ToList();
        for (var position = 0; position < ordered.Count; position++)
        {
            var result = ordered[position];
            var strengths = Objectives
                .Where((objective, index) => ranges[index].Max - ranges[index].Min >= 1e-9 && Math.Abs(objective.Value(result) - ranges[index].Min) < 1e-9)
                .Select(objective => objective.Name).ToList();
            var dominatedBy = feasible.Where(other => other != result && Dominates(other, result)).Select(other => other.Scenario).ToList();
            result.Analysis.RankingExplanation =
                $"Rank {position + 1} of {ordered.Count} by weighted normalised score {result.Analysis.RankingScore:0.000} (0 = best on every objective). " +
                (strengths.Count > 0 ? $"Best on {string.Join(", ", strengths)}. " : "") +
                (dominatedBy.Count > 0 ? $"Dominated by {string.Join(", ", dominatedBy)} (no better on any objective and worse on at least one)." : "On the Pareto front.");
        }
    }

    private static bool Dominates(OptimizationScenarioResultDto a, OptimizationScenarioResultDto b)
    {
        var strictlyBetter = false;
        foreach (var objective in Objectives)
        {
            var left = objective.Value(a);
            var right = objective.Value(b);
            if (left > right + 1e-9) return false;
            if (left < right - 1e-9) strictlyBetter = true;
        }
        return strictlyBetter;
    }
}
