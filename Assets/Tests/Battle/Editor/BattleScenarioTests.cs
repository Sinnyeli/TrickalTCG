using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;

// Runs every [BattleScenario] in Assets/Scripts/Testing/BattleScenarios.cs as its own test case,
// named "<ID> <Method>" and grouped into the Regression or Rules category.
// The scenarios live in Assembly-CSharp (editor-only), so this file only finds and invokes them.
public class BattleScenarioTests
{
    private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    public static IEnumerable<TestCaseData> Scenarios()
    {
        foreach (MethodInfo method in GameType("BattleScenarios").GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            object attribute = method
                .GetCustomAttributes(false)
                .FirstOrDefault(a => a.GetType().Name == "BattleScenarioAttribute");
            if (attribute == null)
                continue;

            string id = (string)Property(attribute, "Id");
            yield return new TestCaseData(method.Name)
                .SetName($"{id} {method.Name}")
                .SetDescription((string)Property(attribute, "Summary"))
                .SetCategory((string)Property(attribute, "Category"));
        }
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Run(string scenario)
    {
        var sandbox = (IDisposable)Activator.CreateInstance(GameType("BattleSandbox"));
        try
        {
            GameType("BattleScenarios").GetMethod(scenario).Invoke(null, new object[] { sandbox });
        }
        catch (TargetInvocationException error) when (error.InnerException?.GetType().Name == "BattleScenarioFailure")
        {
            Assert.Fail(error.InnerException.Message);
        }
        catch (TargetInvocationException error)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
        }
        finally
        {
            sandbox.Dispose();
        }
    }

    private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
}
