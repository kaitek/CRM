using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace jll.emea.crm.Transliteration
{   

    class ConverterService : IConverter
    {    
     
        private string _source;
        private ITracingService _tracingService;

        private readonly Dictionary<string, string> _patternmapping;

        private readonly Dictionary<string, string> _numericmapping;

        private readonly Dictionary<char, string> _charmapping;

        private readonly List<string> _token;

        private readonly Dictionary<string, string> _ending;


        public ConverterService(ITracingService tracingService)
        {
            _tracingService = tracingService;

            _token = new List<string>
            {
                "Avenue",
                "St.",
                "Square",
                "Highway",
                "avenue",
                "st.",
                "square",
                "highway",
            };
            _ending = new Dictionary<string, string>
            {
                {@"ий", @"iy"},
                {@"ый", @"y"},
                {@"aя", @"aya"},

                {@"яя", @"yaya"},
                {@"ое", @"oye"},
                {@"ее", @"eye"},

                {@"чий", @"chiy"},
                {@"ский", @"skiy"},

                {@"ша", @"sha"},
                {@"цкий", @"tskiy"},
                {@"ный", @"ny"},
                {@"ща", @"scha"},
                {@"мый", @"my"},
                {@"щий", @"schiy"},
            };

            _charmapping = new Dictionary<char, string>
            {
                {'а', @"a"},
                {'А', @"A"},
                {'б', @"b"},
                {'Б', @"B"},
                {'в', @"v"},
                {'В', @"V"},
                {'г', @"g"},
                {'Г', @"G"},
                {'д', @"d"},
                {'Д', @"D"},
                {'е', @"e"},
                {'Е', @"E"},
                {'ё', @"yo"},
                {'Ё', @"Yo"},
                {'ж', @"zh"},
                {'Ж', @"Zh"},
                {'з', @"z"},
                {'З', @"Z"},
                {'и', @"i"},
                {'И', @"I"},
                {'й', @"y"},
                {'Й', @"Y"},
                //{'i', @"i"},
                {'I', @"I"},
                {'к', @"k"},
                {'К', @"K"},
                {'л', @"l"},
                {'Л', @"L"},
                {'м', @"m"},
                {'М', @"M"},
                {'н', @"n"},
                {'Н', @"N"},
                {'о', @"o"},
                {'О', @"O"},
                {'п', @"p"},
                {'П', @"P"},
                {'р', @"r"},
                {'Р', @"R"},
                {'с', @"s"},
                {'С', @"S"},
                {'т', @"t"},
                {'Т', @"T"},
                {'у', @"u"},
                {'У', @"U"},
                {'ф', @"f"},
                {'Ф', @"F"},
                {'х', @"kh"},
                {'Х', @"Kh"},
                {'ц', @"ts"},
                {'Ц', @"Ts"},
                {'ч', @"ch"},
                {'Ч', @"Ch"},
                {'ш', @"sh"},
                {'Ш', @"Sh"},
                {'щ', @"shh"},
                {'Щ', @"Shh"},
                {'ъ', string.Empty},
                {'Ъ', string.Empty},
                {'ы', @"y"},
                {'Ы', @"Y"},
                {'ь', string.Empty},
                {'Ь', string.Empty},
                {'э', @"e"},
                {'Э', @"E"},
                {'ю', @"yu"},
                {'Ю', @"Yu"},
                {'я', @"ya"},
                {'Я', @"Ya"}
            };

            _patternmapping = new Dictionary<string, string>
            {
                { "Федеральный округ", "Federal District" },
                { "Субъект федерации", "Federal Subject" },
                { "Республика", "Republic" },
                { "Край", "Territory" },
                { "Область", "Region" },
                { "Город федерального значения", "Federal City" },
                { "Автономная область", "Autonomous Region" },
                { "Автономный округ", "Autonomous District" },
                { "Район", "District" },
                { "Административный округ", "Administrative District" },

                { "Автономная", "Autonomous" },
                { "Автономный", "Autonomous" },
                { "область", "Region" },
                { "округ", "District" },
                { "Административный", "Administrative" },


                { "Улица", "Street" },
                { "Переулок", "Lane" },
                { "Проспект", "Avenue" },
                { "Шоссе", "Highway" },
                { "Бульвар", "Boulevard" },
                { "Проезд", "Passage" },
                { "Площадь", "Square" },
                { "Набережная", "Embankment" },
                { "Тупик", "Blind Alley" },
                { "Аллея", "Alley" },
                { "Просек", "Path" },
                { "Квартал", "Block" },
                { "Линия", "Line" },
                { "Дорога", "Road" },
                { "Магистраль", "Main Road" },
                { "Проулок", "Lane" },
                { "Тракт", "High Road" },
                { "Въезд", "Drive" },
                { "Заезд", "Drive" },
                { "Спуск", "Slope" },
                { "Съезд", "Descent" },
                { "Разъезд", "Furcation" },
                { "Луч", "Ray" },
                { "Кольцо", "Ring" },
                { "Взвоз", "Drive" },

                { "улица", "Street" },
                { "переулок", "Lane" },
                { "проспект", "Avenue" },
                { "шоссе", "Highway" },
                { "бульвар", "Boulevard" },
                { "проезд", "Passage" },
                { "площадь", "Square" },
                { "набережная", "Embankment" },
                { "тупик", "Blind Alley" },
                { "аллея", "Alley" },
                { "просек", "Path" },
                { "квартал", "Block" },
                { "линия", "Line" },
                { "дорога", "Road" },
                { "магистраль", "Main Road" },
                { "проулок", "Lane" },
                { "тракт", "High Road" },
                { "въезд", "Drive" },
                { "заезд", "Drive" },
                { "спуск", "Slope" },
                { "съезд", "Descent" },
                { "разъезд", "Furcation" },
                { "луч", "Ray" },
                { "кольцо", "Ring" },
                { "взвоз", "Drive" },

                { "ул.", "St." },
                { "пер.", "Ln." },
                { "просп.", "Ave." },
                { "ш.", "Hwy." },
                { "бул.", "Blvd." },
                { "пр.", "Psge." },
                { "пл.", "Sq." },
                { "наб.", "Emb." },
                { "туп.", "Bl. Aly." },
                { "ал.", "Aly." },
                { "дор.", "Rd." },


                { "фаза", "Phase" },
                { "Фаза.", "Phase" },

                { "станция метро", "metro station" },
                { "вокзал", "Railway Station" },
                { "«…»", "‘...’" },


                { "Бульварное кольцо", "The Boulevard Ring" },
                { "Садовое кольцо", "The Garden Ring" },

                { "Третье транспортное кольцо", "The Third Transport Ring" },
                { "Московская кольцевая автомобильная дорога", "Automobile Ring Road" },
                { "МКАД", "MKAD" },
                { "ТТК", "The TTR" },

                { "Первый", "First" },
                { "Второй", "Second" },
                { "Третий", "Third" },
                { "Четвёртый", "Fourth" },
                { "Пятый", "Fifth" },
                { "Шестой", "Sixth" },
                { "Седьмой", "Seventh" },
                { "Восьмой", "Eighth" },
                { "Девятый", "Ninth" },
                { "Десятый", "Tenth" },
                { "Одиннадцатый", "Eleventh" },
                { "Двенадцатый", "Twelfth" },
                { "Тринадцатый", "Thirteenth" },
                { "Четырнадцатый", "Fourteenth" },
                { "Пятнадцатый", "Fifteenth" },               
                { "Шестнадцатый", "Sixteenth" },
                { "Семнадцатый", "Seventeenth" },
                { "Восемнадцатый", "Eighteenth" },
                { "Девятнадцатый", "Nineteenth" },
                { "Двадцатый", "Twentieth" },

                { "Первая", "First" },
                { "Вторая", "Second" },
                { "Третья", "Third" },
                { "Четвёртая", "Fourth" },
                { "Пятая", "Fifth" },
                { "Шестая", "Sixth" },
                { "Седьмая", "Seventh" },
                { "Восьмая", "Eighth" },
                { "Девятая", "Ninth" },
                { "Десятая", "Tenth" },
                { "Одиннадцатая", "Eleventh" },
                { "Двенадцатая", "Twelfth" },
                { "Тринадцатая", "Thirteenth" },
                { "Четырнадцатая", "Fourteenth" },
                { "Пятнадцатая", "Fifteenth" },
                { "Шестнадцатая", "Sixteenth" },
                { "Семнадцатая", "Seventeenth" },
                { "Восемнадцатая", "Eighteenth" },
                { "Девятнадцатая", "Nineteenth" },
                { "Двадцатая", "Twentieth" },


                { "1-й", "1st" },
                { "2-й", "2nd" },
                { "3-й", "3rd" },
                { "4-й", "4th" },
                { "5-й", "5th" },
                { "6-й", "6th" },
                { "7-й", "7th" },
                { "8-й", "8th" },
                { "9-й", "9th" },
                { "10-й", "10th" },
                { "11-й", "11th" },
                { "12-й", "12th" },
                { "13-й", "13th" },
                { "14-й", "14th" },
                { "15-й", "15th" },
                { "16-й", "16th" },
                { "17-й", "17th" },
                { "18-й", "18th" },
                { "19-й", "19th" },
                { "20-й", "20h" },


                { "1-я", "1st" },
                { "2-я", "2nd" },
                { "3-я", "3rd" },
                { "4-я", "4th" },
                { "5-я", "5th" },
                { "6-я", "6th" },
                { "7-я", "7th" },
                { "8-я", "8th" },
                { "9-я", "9th" },
                { "10-я", "10th" },
                { "11-я", "11th" },
                { "12-я", "12th" },
                { "13-я", "13th" },
                { "14-я", "14th" },
                { "15-я", "15th" },
                { "16-я", "16th" },
                { "17-я", "17th" },
                { "18-я", "18th" },
                { "19-я", "19th" },
                { "20-я", "20h" },

                { "стр.", "bldg." },
                { "Строение", "Building" },
                { "Литера", "Letter" },
                { "Владение", "Estate" },
                { "Корпус", "Building" },
                { "корпус", "Building" },
                { "корп.", "bldg." },
                { "лит.", "letter" },
                { "вл.", "estate" },
                { "класс", "class" },
                { "Класс", "Class" },
                { "участок", "land plot" },
                { "Участок", "Land plot" },
                //{ "от", "from" },
                //{ "От", "From" },
            };

            _numericmapping = new Dictionary<string, string>
            {
                { "1 кв.", "Q1" },
                { "2 кв.", "Q2" },
                { "3 кв.", "Q3" },
                { "4 кв.", "Q4" },
                { "1 пол.", "H1" },
                { "2 пол.", "H2" },
                { "м²", "m²" },
                { "кв. м", "m²" },
                { "млн", "million" },
                { "млрд", "billion" },               
                { "долл.", "US" },
                { "руб.", "RUB" },
                { "евро", "EURO" },
           };
        }
      
        private bool NeedToTranslate(string source)
        {
            if (IsNumber(source))
                return false;

            List<bool> list = new List<bool>();

            foreach (char c in source)
            {
                if (!IsNumber(new string(c, 1)))
                {
                    list.Add(new string(c, 1).IsCiryllic());
                }
                else
                {
                    list.Add(false);
                }
            }
            return list.Contains(true);           
        }

      
        private bool IsNumber(string source)
        {
            return float.TryParse(source, out float output);
        }

        public string Normalize(string source)
        {
            _source = source;
            try
            {
                StringBuilder sb = new StringBuilder();

                foreach (KeyValuePair<string, string> kvp in _patternmapping)
                {
                    if(_source.ToLower().IndexOf(kvp.Key.ToLower()) > -1)//(my)
                    {
                        _source = _source.Replace(kvp.Key, kvp.Value);                       
                    }
                }

                foreach (KeyValuePair<string, string> kvp in _numericmapping)
                {
                    if (_source.ToLower().IndexOf(kvp.Key.ToLower()) > -1)//(my)
                    {
                        _source = _source.Replace(kvp.Key, kvp.Value);
                    }
                }
                List<string> peaces = _source.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
              

                foreach(string peace in peaces)
                {
                    bool replaced = false;
                    string _copy = peace;
                    foreach (KeyValuePair<string, string> kvp in _ending)
                    {
                        if (kvp.Key.Length < _copy.Length)
                        {
                            if (_copy.ToLower().IndexOf(kvp.Key, _copy.Length - kvp.Key.Length, kvp.Key.Length) > -1)
                            {
                                replaced = true;
                                _copy = _copy.Replace(kvp.Key, kvp.Value);
                            }
                        }
                    }

                    //if (replaced)
                    //    peace = _copy;

                    if (NeedToTranslate(_copy))
                    {
                        
                        foreach(KeyValuePair<char, string> kvp in _charmapping)
                        {
                            _copy = _copy.Replace(new string(kvp.Key, 1), kvp.Value);
                        }

                        sb.Append(_copy);

                    }
                    else
                    {
                        sb.Append(_copy);
                    }

                    sb.Append(new string((char)32, 1));
                }

               
                string temp = sb.ToString().Trim();

                List<string> check = temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();

                var query = (from item in _token
                             where _token.Contains(check[0])
                             //join item2 in check[0] 
                             select item);

                if (query.Any())
                {
                    //shuffle
                    StringBuilder _sb = new StringBuilder();
                    for (int y = 1; y < check.Count; y++)
                    {
                        _sb.Append(check[y])
                           .Append(new string((char)32, 1));
                    }
                    _sb.Append(check[0]);
                    return _sb.ToString().Trim();
                }
                else
                {
                    return temp;
                }       
            }
            catch(Exception ex)
            {
                _tracingService.Trace(ex.Message);
                return source;
            }
        }
    }
}
