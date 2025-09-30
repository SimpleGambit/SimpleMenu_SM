namespace SimpleMenu.App;

public static class NutritionCatalog
{
    public static readonly IReadOnlyList<NutrientDefinition> All =
    [
        new("energy", "열량", "kcal", true),
        new("water", "수분", "g", false),
        new("carbohydrate", "탄수화물", "g", true),
        new("protein", "단백질", "g", true),
        new("fat", "지방", "g", true),
        new("saturatedFat", "포화지방", "g", true),
        new("transFat", "트랜스지방", "g", false),
        new("fiber", "식이섬유", "g", true),
        new("sugars", "당류", "g", true),
        new("sodium", "나트륨", "mg", true),
        new("cholesterol", "콜레스테롤", "mg", false),
        new("calcium", "칼슘", "mg", false),
        new("iron", "철분", "mg", false),
        new("magnesium", "마그네슘", "mg", false),
        new("phosphorus", "인", "mg", false),
        new("potassium", "칼륨", "mg", false),
        new("zinc", "아연", "mg", false),
        new("copper", "구리", "mg", false),
        new("manganese", "망간", "mg", false),
        new("selenium", "셀레늄", "ug", false),
        new("molybdenum", "몰리브덴", "ug", false),
        new("iodine", "요오드", "ug", false),
        new("vitaminA", "비타민 A", "ug RAE", false),
        new("vitaminC", "비타민 C", "mg", false),
        new("vitaminD", "비타민 D", "ug", false),
        new("vitaminE", "비타민 E", "mg alpha-TE", false),
        new("vitaminK", "비타민 K", "ug", false),
        new("vitaminB1", "비타민 B1", "mg", false),
        new("vitaminB2", "비타민 B2", "mg", false),
        new("vitaminB3", "비타민 B3", "mg NE", false),
        new("vitaminB5", "비타민 B5", "mg", false),
        new("vitaminB6", "비타민 B6", "mg", false),
        new("vitaminB7", "비타민 B7", "ug", false),
        new("vitaminB9", "비타민 B9", "ug DFE", false),
        new("vitaminB12", "비타민 B12", "ug", false),
    ];

    public static readonly IReadOnlyList<NutrientDefinition> Required =
        All.Where(nutrient => nutrient.IsRequired).ToArray();

    public static NutrientDefinition Find(string key)
    {
        return All.First(nutrient => nutrient.Key == key);
    }
}
