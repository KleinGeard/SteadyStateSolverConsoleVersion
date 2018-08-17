using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SteadyStateConsoleVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            decimal[,] mchain =
            {
                {0.65m, 0.15m, 0.1m},
                {0.25m, 0.65m, 0.4m},
                {0.1m,  0.2m,  0.5m},
            };

            MarkovChain m = new MarkovChain(mchain);
            Console.WriteLine(m);

            Console.ReadLine();
        }
    }

    public class MarkovChain
    {
        private decimal[,] Matrix;
        private SteadyStateEquation[] steadyStateEquations;
        private decimal[] solvedSteadyStateEquations;
        private int len;

        public MarkovChain(decimal[,] matrix)
        {
            Matrix = matrix;
            len = matrix.GetLength(0);

            steadyStateEquations = new SteadyStateEquation[len];
            for (int i = 0; i < len; i++)
            {
                decimal[] row = Enumerable.Range(0, len)
                    .Select(x => matrix[i, x])
                    .ToArray();

                steadyStateEquations[i] = new SteadyStateEquation(i, row);
            }
        }

        public SteadyStateValue[] SteadyStateValues()
        {
            return null;
        }


        public override string ToString()
        {
            StringBuilder markovChainString = new StringBuilder();



            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < len; j++)
                    markovChainString.Append($"{Matrix[i, j]}  ");

                markovChainString.AppendLine();
            }

            return markovChainString.ToString();
        }
    }

    public class SteadyStateEquation
    {
        public int Equivalent { get; set; }
        public SteadyStateValue[] SteadyStateValues { get; set; }

        public SteadyStateEquation(int equivalent, decimal[] values)
        {
            Equivalent = equivalent;

            SteadyStateValues = new SteadyStateValue[values.Length];
            for (int i = 0; i < values.Length; i++)
                SteadyStateValues[i] = new SteadyStateValue(i, values[i]);
        }
    }

    public class SteadyStateValue
    {
        public int Pi { get; set; } //as an index
        public decimal Value { get; set; }

        public SteadyStateValue(int pi, decimal value)
        {
            Pi = pi;
            Value = value;
        }
    }

}


/*
class MarkovChain2
{
    private List<List<decimal>> markovChain;
    private List<string> names;
    private List<SteadyStateEquation> steadyStateEquations;
    private List<SolvedSteadyStateValue> solvedSteadyStateValues;

    public MarkovChain2(List<List<decimal>> markovChain)
    {
        this.markovChain = transpose2DList(markovChain);
        GenerateEquations();
        solvedSteadyStateValues = new List<SolvedSteadyStateValue>();
    }

    private List<List<T>> transpose2DList<T>(List<List<T>> list)
    {
        List<List<T>> transposed = Enumerable.Range(0, list.Max(l => l.Count))
            .Select(i => list.Select(l => l.ElementAtOrDefault(i)).ToList())
            .ToList();
        return transposed;
    }

    private void GenerateEquations()
    {
        steadyStateEquations = new List<SteadyStateEquation>();
        for (int i = 0; i < markovChain.Count; i++)
            steadyStateEquations.Add(new SteadyStateEquation(markovChain[i], new SteadyStateValue((i + 1).ToString(), 1)));
    }

    public void Setnames(List<string> names)
    {
        if (names.Count == this.names.Count)
            this.names = names;
        else
            throw new Exception("Length of List 'names' does not match dimensions of markov chain");
    }

    public string findSteadyStates() //TODO: break up into multiple smaller methods
    {
        steadyStateEquations.ForEach(s => s.solve());

        SteadyStateEquation firstEquation = steadyStateEquations.First();
        firstEquation.SteadyStateValues.Clear();
        firstEquation.SteadyStateValues.Add(new SteadyStateValue(steadyStateEquations.First().Equivalent.PiName, 1));

        for (int i = 1; i < steadyStateEquations.Count; i++)
            for (int j = 1; j < steadyStateEquations.Count; j++)
                if (i != j)
                {
                    steadyStateEquations[j].substituteEquation(steadyStateEquations[i]);
                }


        SubstituteIntoOne();

        return ""; // TODO: find out what to return
    }

    private void SubstituteIntoOne() //NOTE: This method assumes that all equations are solved in terms of π1
    {
        decimal sum = 0;
        string equation = "";

        for (int i = 0; i < steadyStateEquations.Count - 1; i++)
        {
            SteadyStateValue subableValue = steadyStateEquations[i].SteadyStateValues.First();
            sum += subableValue.Value;
            equation += $"{subableValue} + ";
        }
        SteadyStateValue lastSubableValue = steadyStateEquations.Last().SteadyStateValues.First();
        sum += lastSubableValue.Value;
        equation += $"{lastSubableValue} = 1";

        writeSubstituteIntoOneTex(sum, equation);

        adjustAll(1 / sum);
    }

    private void writeSubstituteIntoOneTex(decimal sum, string equation)
    {
        string piName = steadyStateEquations.Last().SteadyStateValues.First().PiName;
        decimal roundedSum = Math.Round(sum, 4);
    }

    private void adjustAll(decimal pi1Value)
    {
        foreach (SteadyStateEquation equation in steadyStateEquations)
        {
            decimal relativeValue = equation.SteadyStateValues.First().Value;
            string piName = equation.Equivalent.PiName;

            SolvedSteadyStateValue solvedSteadyStateValue = new SolvedSteadyStateValue(piName, relativeValue * pi1Value);
            solvedSteadyStateValues.Add(solvedSteadyStateValue);
            }
    }

    private class SteadyStateEquation
    {
        public List<SteadyStateValue> SteadyStateValues { get; set; }
        public SteadyStateValue Equivalent { get; set; }

        public SteadyStateEquation(List<decimal> values, SteadyStateValue equivalent)
        {
            SteadyStateValues = new List<SteadyStateValue>();

            for (int i = 0; i < values.Count; i++)
                SteadyStateValues.Add(new SteadyStateValue((i + 1).ToString(), values[i]));
            Equivalent = equivalent;
        }

        public override string ToString()
        {
            string equation = "";

            for (int j = 0; j < SteadyStateValues.Count - 1; j++)
                equation += $"{SteadyStateValues[j]} + ";
            equation += $"{SteadyStateValues.Last()} = {Equivalent}";

            return equation;
        }

        public string ValuesAsString()
        {
            string valueString = "(";

            for (int i = 0; i < SteadyStateValues.Count - 1; i++)
                valueString += $"{SteadyStateValues[i]} + ";

            valueString += $"{SteadyStateValues.Last()})";

            return valueString;
        }

        #region substitution_steps
        public void substituteEquation(SteadyStateEquation subEquation)
        {
            for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                if (SteadyStateValues[i].PiName == (subEquation.Equivalent.PiName))
                {
                    SubstituteValue(i, subEquation);
                }

            Consolidate();
            solve();
        }

        private void SubstituteValue(int oldSteadyStateValueIndex, SteadyStateEquation SubEquation)
        {
            decimal p = SteadyStateValues[oldSteadyStateValueIndex].Value;

            foreach (SteadyStateValue newSteadyStateValue in SubEquation.SteadyStateValues)
                SteadyStateValues.Add(new SteadyStateValue(newSteadyStateValue.PiName, newSteadyStateValue.Value * p));

            SteadyStateValues.RemoveAt(oldSteadyStateValueIndex);
        }

        #region solve
        public void solve(bool fromSubstituteEquation = true)
        {
            bool needsSolving = false;
            SolveStepOne(ref needsSolving);
            if (!needsSolving || Equivalent.Value == 1)
            {
                return;
            }

            string equationString = "";
            SolveStepTwo(ref equationString);
        }

        private void SolveStepOne(ref bool needsSolving) //NOTE: not entirely necessary unless showing working is required
        {
            //step 1: take relevant value out
            for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                if (SteadyStateValues[i].PiName.Equals(Equivalent.PiName))
                {
                    Equivalent.Value = 1 - SteadyStateValues[i].Value;
                    SteadyStateValues.RemoveAt(i);
                    needsSolving = true;
                    break;
                }
        }

        private void SolveStepTwo(ref string equationString)
        {
            //step 2: adjust such that the equiv = 1
            for (int i = 0; i < SteadyStateValues.Count - 1; i++)
            {
                equationString += $"{{{SteadyStateValues[i]} \\over {Equivalent.getRoundedValue()}}} + ";
                SteadyStateValues[i].Value /= Equivalent.Value;
            }
            equationString += $"{{{SteadyStateValues.Last()} \\over {Equivalent.getRoundedValue()}}} = \\pi_{Equivalent.PiName}";
            SteadyStateValues.Last().Value /= Equivalent.Value; //BUG: Can't divide by 0

            Equivalent.Value = 1;
        }
        #endregion solve

        public void Consolidate()
        {
            List<int> removalIndices = new List<int>();

            for (int i = SteadyStateValues.Count - 1; i >= 0; i--)
                for (int j = SteadyStateValues.Count - 1; j >= 0; j--)
                    if (i != j && SteadyStateValues[i].PiName.Equals(SteadyStateValues[j].PiName) && !removalIndices.Contains(j))
                    {
                        decimal p = SteadyStateValues[i].Value;
                        removalIndices.Add(i);
                        SteadyStateValues[j].Value += p;
                    }

            removalIndices.ForEach(i => SteadyStateValues.RemoveAt(i));
        }
        #endregion substitution_steps
    }

    private class SteadyStateValue
    {
        public string PiName { get; set; }
        public decimal Value { get; set; }

        public SteadyStateValue(string piName, decimal value)
        {
            PiName = piName;
            Value = value;
        }

        public override string ToString()
        {
            return (Value == 1) ? $"\\pi_{PiName}" : $"{getRoundedValue()}\\pi_{PiName}";
        }

        public decimal getRoundedValue()
        {
            return Math.Round(Value, 4);
        }
    }

    private class SolvedSteadyStateValue : SteadyStateValue
    {
        public SolvedSteadyStateValue(string piName, decimal value) : base(piName, value) { }

        public override string ToString()
        {
            return $"\\pi_{PiName} = {getRoundedValue()}";
        }
    }

}

}
*/
  