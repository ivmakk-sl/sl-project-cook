using System.Collections.Generic;
using ProjectCook;
using Xunit;

public class EatLogicTests
{
    private const int Sat = 0, Mor = 1, Sta = 2, Fit = 3, Life = 4;
    private const int Meat = 2, Staple = 1, Fruit = 6;

    private static EatLogic.Dish Dish(float[] values, int subCategory = Meat, bool instance = true, bool staple = false, bool perfect = false, int portions = 1) =>
        new EatLogic.Dish { Values = values, HasInstanceValues = instance, SubCategory = subCategory, IsStaple = staple, IsPerfect = perfect, Portions = portions };

    private static EatLogic.Factors With(params EatLogic.Talent[] talents) => new EatLogic.Factors { Talents = new List<EatLogic.Talent>(talents) };

    [Fact]
    public void NoTalent_IsTheBase_AndFitnessIs0()
    {
        var v = EatLogic.EatValues(Dish(new float[] { 40, 4, 10, 6, 5 }), new EatLogic.Factors());
        Assert.Equal(new float[] { 40, 4, 10, 0, 5 }, v);
    }

    [Fact]
    public void NourishingRichness_40_Is42()
    {
        var v = EatLogic.EatValues(Dish(new float[] { 40, 0, 0, 0, 0 }), With(new EatLogic.Talent { Name = "Nourishing Richness I", Nourish = 0.05f }));
        Assert.Equal(42, v[Sat], 3);
    }

    [Fact]
    public void Nourish_NeedsInstanceValues()
    {
        var v = EatLogic.EatValues(Dish(new float[] { 40, 0, 0, 0, 0 }, instance: false), With(new EatLogic.Talent { Nourish = 0.05f }));
        Assert.Equal(40, v[Sat], 3);
    }

    [Fact]
    public void PerfectMorale_OnceForEachPortion()
    {
        var factors = With(new EatLogic.Talent { Name = "Perfect Morale I", PerfectMorale = 3 });
        Assert.Equal(10, EatLogic.EatValues(Dish(new float[] { 0, 4, 0, 0, 0 }, perfect: true, portions: 2), factors)[Mor], 3);
        Assert.Equal(4, EatLogic.EatValues(Dish(new float[] { 0, 4, 0, 0, 0 }, perfect: false, portions: 2), factors)[Mor], 3);
    }

    [Fact]
    public void PerfectMorale_GetsTheMoraleFactors()
    {
        var factors = With(new EatLogic.Talent { PerfectMorale = 3 }, new EatLogic.Talent { AddPermille = new[] { 0, 100, 0, 0, 0 } });
        // (4 + 3) x 1.1
        Assert.Equal(7.7f, EatLogic.EatValues(Dish(new float[] { 0, 4, 0, 0, 0 }, perfect: true), factors)[Mor], 3);
    }

    [Fact]
    public void Difficulty_Minus25Percent_Life4_Is3()
    {
        var factors = new EatLogic.Factors { LifeDelta = -0.25f };
        Assert.Equal(3, EatLogic.EatValues(Dish(new float[] { 0, 0, 0, 0, 4 }), factors)[Life], 3);
    }

    [Fact]
    public void Difficulty_LifeIsNotBelow0()
    {
        var factors = new EatLogic.Factors { LifeDelta = -1.5f };
        Assert.Equal(0, EatLogic.EatValues(Dish(new float[] { 0, 0, 0, 0, 4 }), factors)[Life], 3);
    }

    [Fact]
    public void FoodTypeTalent_OnlyOnItsType()
    {
        var sat = new float[12];
        sat[Meat] = 0.75f;
        var mor = new float[12];
        mor[Meat] = 0.25f;
        var factors = With(new EatLogic.Talent { Name = "Carnivore", SatietyFood = sat, MoraleFood = mor });

        var meat = EatLogic.EatValues(Dish(new float[] { 20, 4, 0, 0, 0 }, Meat), factors);
        Assert.Equal(35, meat[Sat], 3);
        Assert.Equal(5, meat[Mor], 3);
        var fruit = EatLogic.EatValues(Dish(new float[] { 20, 4, 0, 0, 0 }, Fruit), factors);
        Assert.Equal(20, fruit[Sat], 3);
        Assert.Equal(4, fruit[Mor], 3);
    }

    [Fact]
    public void FoodType_ThenTheAddRatio()
    {
        var sat = new float[12];
        sat[Meat] = 0.5f;
        var factors = With(new EatLogic.Talent { SatietyFood = sat }, new EatLogic.Talent { AddPermille = new[] { 100, 0, 0, 0, 0 } });
        // 20 x 1.5 x 1.1
        Assert.Equal(33, EatLogic.EatValues(Dish(new float[] { 20, 0, 0, 0, 0 }, Meat), factors)[Sat], 3);
    }

    [Fact]
    public void ComfortEater_OnAStapleOnly()
    {
        var factors = With(new EatLogic.Talent { Name = "Comfort Eater", ComfortPermille = 120 });
        Assert.Equal(56, EatLogic.EatValues(Dish(new float[] { 50, 0, 0, 0, 0 }, Staple, staple: true), factors)[Sat], 3);
        Assert.Equal(50, EatLogic.EatValues(Dish(new float[] { 50, 0, 0, 0, 0 }, Meat), factors)[Sat], 3);
    }

    [Fact]
    public void EfficientDiet_OnAnyFood_OnlySatiety()
    {
        var factors = With(new EatLogic.Talent { Name = "Efficient Diet", EfficientPermille = 150 });
        var v = EatLogic.EatValues(Dish(new float[] { 50, 10, 0, 0, 0 }, Fruit), factors);
        Assert.Equal(57.5f, v[Sat], 3);
        Assert.Equal(10, v[Mor], 3);
    }

    [Fact]
    public void ConditionTalents_AddInsideTheClampOfTheAddRatio()
    {
        // 1900 + 150 = 2050 per mille, clamped to 2000: x3
        var factors = With(new EatLogic.Talent { AddPermille = new[] { 1900, 0, 0, 0, 0 } }, new EatLogic.Talent { EfficientPermille = 150 });
        Assert.Equal(30, EatLogic.EatValues(Dish(new float[] { 10, 0, 0, 0, 0 }), factors)[Sat], 3);
    }

    [Fact]
    public void AddRatio_IsClampedAtMinus50Percent()
    {
        var factors = new EatLogic.Factors { BasePermille = new[] { -800, 0, 0, 0, 0 } };
        Assert.Equal(5, EatLogic.EatValues(Dish(new float[] { 10, 0, 0, 0, 0 }), factors)[Sat], 3);
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.1f, 100)]
    [InlineData(-0.25f, -250)]
    [InlineData(2f, 2000)]
    public void ToPermille_IsTheGameFloatTimes1000(float value, int permille)
    {
        Assert.Equal(permille, EatLogic.ToPermille(value));
    }

    [Fact]
    public void BaseRatio_Plus10Percent_40Is44()
    {
        var factors = new EatLogic.Factors { BasePermille = new[] { EatLogic.ToPermille(0.1f), 0, 0, 0, 0 } };
        Assert.Equal(44, EatLogic.EatValues(Dish(new float[] { 40, 0, 0, 0, 0 }), factors)[Sat], 3);
    }

    [Fact]
    public void FoodType_IsClampedBetweenMinus1And2()
    {
        var sat = new float[12];
        sat[Meat] = 3f;
        Assert.Equal(30, EatLogic.EatValues(Dish(new float[] { 10, 0, 0, 0, 0 }), With(new EatLogic.Talent { SatietyFood = sat }))[Sat], 3);
    }

    [Fact]
    public void ALoss_GetsNoGainFactor()
    {
        var mor = new float[12];
        mor[Meat] = 0.5f;
        var factors = With(new EatLogic.Talent { AddPermille = new[] { 0, 100, 0, 0, 0 }, MoraleFood = mor, Nourish = 0f });
        Assert.Equal(-3, EatLogic.EatValues(Dish(new float[] { 0, -3, 0, 0, 0 }), factors)[Mor], 3);
    }
}
