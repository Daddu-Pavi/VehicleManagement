using VehicleManagement.Core.Entities;
using VehicleManagement.Core.Interfaces;

namespace VehicleManagement.Core.Services;

public class CategoryService : ICategoryService
{
    // =====================================================
    // DETERMINE CATEGORY FOR VEHICLE WEIGHT
    // =====================================================

    public VehicleCategory? GetCategoryForWeight(
        decimal weight,
        IEnumerable<VehicleCategory> categories)
    {
        if (weight <= 0)
        {
            return null;
        }

        return categories
            .OrderBy(c => c.MinWeight)
            .SingleOrDefault(c =>
                weight >= c.MinWeight &&
                (c.MaxWeight == null ||
                 weight < c.MaxWeight));
    }


    // =====================================================
    // VALIDATE COMPLETE CATEGORY CONFIGURATION
    // =====================================================

    public IReadOnlyList<string> ValidateConfiguration(
        IEnumerable<VehicleCategory> categories)
    {
        var errors = new List<string>();

        var orderedCategories = categories
            .OrderBy(c => c.MinWeight)
            .ToList();

        if (orderedCategories.Count == 0)
        {
            errors.Add("At least one category is required.");
            return errors;
        }


        foreach (var category in orderedCategories)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                errors.Add("Category name is required.");
            }

            if (string.IsNullOrWhiteSpace(category.Icon))
            {
                errors.Add(
                    $"Category '{category.Name}' requires an icon.");
            }

            if (category.MinWeight < 0)
            {
                errors.Add(
                    $"Category '{category.Name}' has an invalid minimum weight.");
            }

            if (category.MaxWeight.HasValue &&
                category.MaxWeight.Value <= category.MinWeight)
            {
                errors.Add(
                    $"Category '{category.Name}' must have a maximum weight greater than its minimum weight.");
            }
        }


        // Coverage must start at 0 kg.
        if (orderedCategories[0].MinWeight != 0)
        {
            errors.Add(
                "Category ranges must start at 0 kg.");
        }


        // Neighbouring categories must meet exactly.
        for (var i = 0;
             i < orderedCategories.Count - 1;
             i++)
        {
            var current = orderedCategories[i];
            var next = orderedCategories[i + 1];

            if (current.MaxWeight == null)
            {
                errors.Add(
                    $"Category '{current.Name}' cannot be unbounded because another category follows it.");

                continue;
            }

            if (current.MaxWeight < next.MinWeight)
            {
                errors.Add(
                    $"There is a gap between '{current.Name}' and '{next.Name}'.");
            }
            else if (current.MaxWeight > next.MinWeight)
            {
                errors.Add(
                    $"There is an overlap between '{current.Name}' and '{next.Name}'.");
            }
        }


        // Final category must cover all remaining weights.
        if (orderedCategories[^1].MaxWeight != null)
        {
            errors.Add(
                "The final category must have no maximum weight.");
        }

        return errors;
    }


    // =====================================================
    // BUILD ADD CATEGORY PREVIEW
    // =====================================================

    public CategoryAddPreview BuildAddPreview(
        string name,
        string icon,
        decimal minWeight,
        decimal? maxWeight,
        IEnumerable<VehicleCategory> categories)
    {
        var result = new CategoryAddPreview();


        // Work with copies so Preview does not modify
        // the database entities.
        var currentCategories = categories
            .OrderBy(c => c.MinWeight)
            .Select(c => new VehicleCategory
            {
                Id = c.Id,
                Name = c.Name,
                MinWeight = c.MinWeight,
                MaxWeight = c.MaxWeight,
                Icon = c.Icon
            })
            .ToList();


        // =================================================
        // BASIC VALIDATION
        // =================================================

        if (string.IsNullOrWhiteSpace(name))
        {
            result.ErrorMessage =
                "Category name is required.";

            return result;
        }


        if (string.IsNullOrWhiteSpace(icon))
        {
            result.ErrorMessage =
                "Category icon is required.";

            return result;
        }


        if (minWeight < 0)
        {
            result.ErrorMessage =
                "Lower boundary cannot be negative.";

            return result;
        }


        if (maxWeight.HasValue &&
            maxWeight.Value <= minWeight)
        {
            result.ErrorMessage =
                "Upper boundary must be greater than the lower boundary.";

            return result;
        }


        if (currentCategories.Any(c =>
                c.Name.Equals(
                    name.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
        {
            result.ErrorMessage =
                "A category with this name already exists.";

            return result;
        }


        if (currentCategories.Count == 0)
        {
            if (minWeight != 0 ||
                maxWeight.HasValue)
            {
                result.ErrorMessage =
                    "The first category must start at 0 kg and have no maximum weight.";

                return result;
            }

            var firstCategory =
                new VehicleCategory
                {
                    Name = name.Trim(),
                    Icon = icon,
                    MinWeight = 0,
                    MaxWeight = null
                };

            result.ProposedCategories.Add(
                firstCategory);

            result.Changes.Add(
                $"{firstCategory.Name}: new category " +
                $"{FormatRange(
                    firstCategory.MinWeight,
                    firstCategory.MaxWeight)}");

            result.CanProceed = true;

            return result;
        }


        // =================================================
        // CASE 1:
        // ADD NEW FINAL OPEN-ENDED CATEGORY
        //
        // Example:
        //
        // Heavy       1800 - ∞
        //
        // Add:
        // Super Heavy 5000 - ∞
        //
        // Result:
        // Heavy       1800 - 5000
        // Super Heavy 5000 - ∞
        // =================================================

        if (!maxWeight.HasValue)
        {
            var finalCategory =
                currentCategories
                    .OrderBy(c => c.MinWeight)
                    .Last();

            if (finalCategory.MaxWeight != null)
            {
                result.ErrorMessage =
                    "The current final category configuration is invalid.";

                return result;
            }


            if (minWeight <= finalCategory.MinWeight)
            {
                result.ErrorMessage =
                    $"The new final category must start above " +
                    $"{finalCategory.MinWeight:0.##} kg.";

                return result;
            }


            var oldMax =
                finalCategory.MaxWeight;


            finalCategory.MaxWeight =
                minWeight;


            result.Changes.Add(
                $"{finalCategory.Name}: " +
                $"{FormatRange(
                    finalCategory.MinWeight,
                    oldMax)} → " +
                $"{FormatRange(
                    finalCategory.MinWeight,
                    minWeight)}");


            var newFinalCategory =
                new VehicleCategory
                {
                    Name = name.Trim(),
                    Icon = icon,
                    MinWeight = minWeight,
                    MaxWeight = null
                };


            currentCategories.Add(
                newFinalCategory);


            var proposedFinalConfiguration =
                currentCategories
                    .OrderBy(c => c.MinWeight)
                    .ToList();


            var finalErrors =
                ValidateConfiguration(
                    proposedFinalConfiguration);


            if (finalErrors.Count > 0)
            {
                result.ErrorMessage =
                    string.Join(
                        " ",
                        finalErrors);

                return result;
            }


            result.Changes.Add(
                $"{newFinalCategory.Name}: new category " +
                $"{FormatRange(
                    newFinalCategory.MinWeight,
                    newFinalCategory.MaxWeight)}");


            result.ProposedCategories =
                proposedFinalConfiguration;

            result.CanProceed = true;

            return result;
        }


        // From this point the category has a normal
        // finite upper boundary.
        var boundedMaxWeight =
            maxWeight.Value;


        // =================================================
        // FIND ORIGINAL CATEGORIES CONTAINING
        // LOWER AND UPPER BOUNDARIES
        // =================================================

        var lowerOriginal =
            currentCategories
                .SingleOrDefault(c =>
                    minWeight >= c.MinWeight &&
                    (c.MaxWeight == null ||
                     minWeight < c.MaxWeight));


        var upperOriginal =
            currentCategories
                .SingleOrDefault(c =>
                    boundedMaxWeight > c.MinWeight &&
                    (c.MaxWeight == null ||
                     boundedMaxWeight <= c.MaxWeight));


        if (lowerOriginal == null ||
            upperOriginal == null)
        {
            result.ErrorMessage =
                "The proposed boundaries could not be matched " +
                "to the current category configuration.";

            return result;
        }


        // Capture original upper boundary BEFORE any
        // copied category is modified.
        var lowerOriginalMax =
            lowerOriginal.MaxWeight;


        // =================================================
        // DETERMINE COMPLETELY COVERED CATEGORIES
        // =================================================

        var categoriesToDelete =
            currentCategories
                .Where(c =>
                    c.MinWeight >= minWeight &&
                    c.MaxWeight.HasValue &&
                    c.MaxWeight.Value <=
                    boundedMaxWeight)
                .ToList();


        result.CategoriesToDelete =
            categoriesToDelete;


        // =================================================
        // BUILD REMAINING CONFIGURATION
        // =================================================

        var proposed =
            currentCategories
                .Where(c =>
                    !categoriesToDelete.Any(
                        deleted =>
                            deleted.Id == c.Id))
                .ToList();


        // =================================================
        // CATEGORY BEFORE NEW CATEGORY
        // =================================================

        var lowerNeighbour =
            proposed
                .Where(c =>
                    c.MinWeight < minWeight)
                .OrderByDescending(c =>
                    c.MinWeight)
                .FirstOrDefault();


        if (lowerNeighbour != null)
        {
            var oldMax =
                lowerNeighbour.MaxWeight;


            if (oldMax != minWeight)
            {
                lowerNeighbour.MaxWeight =
                    minWeight;


                result.Changes.Add(
                    $"{lowerNeighbour.Name}: " +
                    $"{FormatRange(
                        lowerNeighbour.MinWeight,
                        oldMax)} → " +
                    $"{FormatRange(
                        lowerNeighbour.MinWeight,
                        minWeight)}");
            }
        }
        else if (minWeight != 0)
        {
            result.ErrorMessage =
                "The proposed range would leave weights below " +
                "the new category without coverage.";

            return result;
        }


        // =================================================
        // CATEGORY AFTER NEW CATEGORY
        // =================================================

        VehicleCategory? upperNeighbour;


        // Both boundaries are inside the same existing
        // category.
        //
        // Because one category cannot represent two
        // disconnected ranges, the following category
        // takes over after the new category.
        if (lowerOriginal.Id ==
                upperOriginal.Id &&
            minWeight >
                lowerOriginal.MinWeight &&
            lowerOriginalMax.HasValue &&
            boundedMaxWeight <
                lowerOriginalMax.Value)
        {
            upperNeighbour =
                proposed
                    .Where(c =>
                        c.Id != lowerOriginal.Id &&
                        c.MinWeight >=
                        lowerOriginalMax.Value)
                    .OrderBy(c =>
                        c.MinWeight)
                    .FirstOrDefault();


            if (upperNeighbour == null)
            {
                result.ErrorMessage =
                    "The new category cannot be inserted here because " +
                    "there is no following category that can cover " +
                    "the remaining upper weight range.";

                return result;
            }
        }
        else
        {
            upperNeighbour =
                proposed
                    .Where(c =>
                        c.Id != lowerNeighbour?.Id &&
                        (c.MaxWeight == null ||
                         c.MaxWeight.Value >
                         boundedMaxWeight))
                    .OrderBy(c =>
                        c.MinWeight)
                    .FirstOrDefault();
        }


        if (upperNeighbour != null)
        {
            var oldMin =
                upperNeighbour.MinWeight;


            if (oldMin !=
                boundedMaxWeight)
            {
                upperNeighbour.MinWeight =
                    boundedMaxWeight;


                result.Changes.Add(
                    $"{upperNeighbour.Name}: " +
                    $"{FormatRange(
                        oldMin,
                        upperNeighbour.MaxWeight)} → " +
                    $"{FormatRange(
                        boundedMaxWeight,
                        upperNeighbour.MaxWeight)}");
            }
        }
        else
        {
            result.ErrorMessage =
                "The proposed range would leave weights above " +
                "the new category without coverage.";

            return result;
        }


        // =================================================
        // RECORD CATEGORIES THAT WILL BE DELETED
        // =================================================

        foreach (var category in
                 categoriesToDelete)
        {
            result.Changes.Add(
                $"{category.Name}: DELETE " +
                $"({FormatRange(
                    category.MinWeight,
                    category.MaxWeight)})");
        }


        // =================================================
        // ADD NEW CATEGORY
        // =================================================

        var newCategory =
            new VehicleCategory
            {
                Name = name.Trim(),
                Icon = icon,
                MinWeight = minWeight,
                MaxWeight =
                    boundedMaxWeight
            };


        proposed.Add(
            newCategory);


        proposed =
            proposed
                .OrderBy(c =>
                    c.MinWeight)
                .ToList();


        // =================================================
        // FINAL BUSINESS-RULE VALIDATION
        // =================================================

        var errors =
            ValidateConfiguration(
                proposed);


        if (errors.Count > 0)
        {
            result.ErrorMessage =
                string.Join(
                    " ",
                    errors);

            return result;
        }


        result.Changes.Add(
            $"{newCategory.Name}: new category " +
            $"{FormatRange(
                newCategory.MinWeight,
                newCategory.MaxWeight)}");


        result.ProposedCategories =
            proposed;

        result.CanProceed = true;

        return result;
    }


    // =====================================================
    // FORMAT RANGE
    // =====================================================

    private static string FormatRange(
        decimal min,
        decimal? max)
    {
        return max.HasValue
            ? $"{min:0.##} – {max.Value:0.##} kg"
            : $"{min:0.##} kg and above";
    }
}