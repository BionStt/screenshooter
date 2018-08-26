using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenShooter.Gun.Extensions
{
    internal static class StringExtension
    {
        private static readonly Dictionary<Char, String> Map = new Dictionary<Char, String>
                                                               {
                                                                   {'а', "a"},
                                                                   {'б', "b"},
                                                                   {'в', "v"},
                                                                   {'г', "g"},
                                                                   {'д', "d"},
                                                                   {'е', "e"},
                                                                   {'ё', "e"},
                                                                   {'ж', "zh"},
                                                                   {'з', "z"},
                                                                   {'и', "i"},
                                                                   {'й', "i"},
                                                                   {'к', "k"},
                                                                   {'л', "l"},
                                                                   {'м', "m"},
                                                                   {'н', "n"},
                                                                   {'о', "o"},
                                                                   {'п', "p"},
                                                                   {'р', "r"},
                                                                   {'с', "s"},
                                                                   {'т', "t"},
                                                                   {'у', "u"},
                                                                   {'ф', "f"},
                                                                   {'х', "kh"},
                                                                   {'ц', "ts"},
                                                                   {'ч', "ch"},
                                                                   {'ш', "sh"},
                                                                   {'щ', "sch"},
                                                                   {'ъ', String.Empty},
                                                                   {'ы', "y"},
                                                                   {'ь', String.Empty},
                                                                   {'э', "e"},
                                                                   {'ю', "yu"},
                                                                   {'я', "ya"},
                                                                   {'\"', String.Empty},
                                                                   {'\'', String.Empty},
                                                                   {'<', String.Empty},
                                                                   {'>', String.Empty},
                                                                   {'@', String.Empty},
                                                                   {'#', String.Empty},
                                                                   {'$', String.Empty},
                                                                   {'!', String.Empty},
                                                                   {'?', String.Empty},
                                                                   {'(', String.Empty},
                                                                   {')', String.Empty},
                                                                   {':', String.Empty},
                                                                   {';', String.Empty},
                                                                   {'=', String.Empty},
                                                                   {'~', String.Empty},
                                                                   {'{', String.Empty},
                                                                   {'}', String.Empty},
                                                                   {']', String.Empty},
                                                                   {'[', String.Empty},
                                                                   {'*', String.Empty},
                                                                   {'\\', String.Empty},
                                                                   {'/', String.Empty},
                                                                   {'|', String.Empty},
                                                                   {'^', String.Empty},
                                                               };


        internal static String ToUrlStandard(this String input)
        {
            var stringBuilder = new StringBuilder(input.Length);

            foreach (var c in input)
            {
                if (Char.IsWhiteSpace(c))
                {
                    if (!stringBuilder[stringBuilder.Length - 1].Equals('-'))
                    {
                        stringBuilder.Append('-');
                    }
                }
                else if (Char.IsDigit(c) ||
                         Char.IsNumber(c))
                {
                    stringBuilder.Append(c);
                }
                else if (!Map.ContainsKey(c))
                {
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append(Map[c]);
                }
            }

            return stringBuilder.ToString();
        }
    }
}