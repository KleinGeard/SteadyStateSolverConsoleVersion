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
            //List<List<decimal>> mchain = new List<List<decimal>>
            //{
            //    new List<decimal>{0.75m, 0m, 0.25m},
            //    new List<decimal>{0.25m, 2/3m, 0.25m},
            //    new List<decimal>{0m,  1/3m,  0.5m},
            //};
            List<List<decimal>> mchain = new List<List<decimal>>
            {
                new List<decimal>{0.65m, 0.15m, 0.1m},
                new List<decimal>{0.25m, 0.65m, 0.4m},
                new List<decimal>{0.1m,  0.2m,  0.5m},
            };
            //List<List<decimal>> mchain = new List<List<decimal>>
            //{
            //    new List<decimal>{0m, 2/5m, 1/3m, 0m},
            //    new List<decimal>{2/3m, 0m, 1/3m, 2/3m},
            //    new List<decimal>{1/3m, 1/5m, 0m, 1/3m},
            //    new List<decimal>{0m, 2/5m, 1/3m, 0m},
            //};
            MarkovChain markovChain = new MarkovChain(mchain);
            Console.WriteLine(markovChain.findSteadyStates());
            Console.ReadLine();
        }
    }

    class MarkovChain
    {
        private List<List<decimal>> markovChain;
        private List<string> names;
        private List<SteadyStateEquation> steadyStateEquations;
        private List<SolvedSteadyStateValue> solvedSteadyStateValues;
        private static StringBuilder texString;

        public MarkovChain(List<List<decimal>> markovChain)
        {
            this.markovChain = transpose2DList(markovChain);
            GenerateEquations();
            solvedSteadyStateValues = new List<SolvedSteadyStateValue>();
            texString = new StringBuilder();
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

        private void writeEquations()
        {
            steadyStateEquations.ForEach(s => texString.AppendLine($"$${s.ToString()}$$"));
        }

        private void writeSolvedValues()
        {
            solvedSteadyStateValues.ForEach(v => texString.AppendLine($"$${v.ToString()}$$"));
        }

        private static void writeEquationToTex(string s)
        {
            texString.AppendLine($"$${s}$$");
        }
        private static void writeToTex(string s)
        {
            texString.AppendLine(s);
        }

        public string findSteadyStates() //TODO: break up into multiple smaller methods
        {
            writeToTex("The initial equations are as such:");
            writeEquations();

            writeToTex("Remove \\(\\pi_k\\) from the equations. ");
            steadyStateEquations.ForEach(s => s.solve());

            writeToTex("The resulting equations are as such:");
            writeEquations();

            SteadyStateEquation firstEquation = steadyStateEquations.First();
            firstEquation.SteadyStateValues.Clear();
            firstEquation.SteadyStateValues.Add(new SteadyStateValue(steadyStateEquations.First().Equivalent.PiName, 1));

            writeToTex($"It is known that \\({firstEquation.Equivalent} = {firstEquation.Equivalent}\\), so this can be solved straight away.");
            writeEquations();

            for (int i = 1; i < steadyStateEquations.Count; i++)
                for (int j = 1; j < steadyStateEquations.Count; j++)
                    if (i != j)
                    {
                        writeToTex($"Substitute \\({steadyStateEquations[i].Equivalent}\\) into \\({steadyStateEquations[j].Equivalent}\\).");
                        steadyStateEquations[j].substituteEquation(steadyStateEquations[i]);
                    }

            writeToTex($"The steady state values in terms of \\({firstEquation.Equivalent}\\) are as such:");
            writeEquations();

            SubstituteIntoOne();

            writeToTex($"The solved steady state values are as such:");
            writeSolvedValues();

            return Regex.Replace(texString.ToString(), @"[\r\n]{3,}", "\r\n\r\n");
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

            writeToTex($"Substitute \\(\\pi_{piName}\\) into 1");
            writeEquationToTex(equation);
            writeEquationToTex($"{roundedSum}\\pi_{piName} = 1");
            writeEquationToTex($"\\pi_{piName} = {{1 \\over {roundedSum}\\pi_{piName}}}");
            writeEquationToTex($"\\pi_{piName} = {Math.Round(1 / sum, 4)}");
        }

        private void adjustAll(decimal pi1Value)
        {
            writeToTex("Solve remaining steady state values");

            foreach (SteadyStateEquation equation in steadyStateEquations)
            {
                decimal relativeValue = equation.SteadyStateValues.First().Value;
                string piName = equation.Equivalent.PiName;

                SolvedSteadyStateValue solvedSteadyStateValue = new SolvedSteadyStateValue(piName, relativeValue * pi1Value);
                solvedSteadyStateValues.Add(solvedSteadyStateValue);

                writeEquationToTex($"\\pi_{piName} = {Math.Round(relativeValue, 4)} \\times {Math.Round(pi1Value, 4)} = {solvedSteadyStateValue.getRoundedValue()}");
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
                        writeEquationToTex(ToString().Replace(SteadyStateValues[i].ToString(), SteadyStateValues[i].getRoundedValue() + subEquation.ValuesAsString()));
                        SubstituteValue(i, subEquation);
                        writeEquationToTex(ToString());
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
                writeEquationToTex(ToString());

                string equationString = "";
                SolveStepTwo(ref equationString);
                writeEquationToTex(equationString);
                writeEquationToTex(ToString());
                writeEquationToTex("$$  $$");
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

                writeEquationToTex(ToString());
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
