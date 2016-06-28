using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Xml2CSharp
{
    public static class XElementExtension
    {
        public static IEnumerable<Class> ExtractClassInfo(this XElement element)
        {
            var @classes = new HashSet<Class>();
            ElementToClass(element, classes);
            return @classes;
        }

        public static bool IsEmpty(this XElement element)
        {
            return !element.HasAttributes && !element.HasElements;
        }

        private static Class ElementToClass(XElement xElement, ICollection<Class> classes)
        {
            var completeName = GetCompleteName(xElement, xElement.Parent);

            var @class = new Class
            {
                Name = completeName,
                XmlName = xElement.Name.LocalName,
                Fields = ReplaceDuplicatesWithLists(ExtractFields(xElement, classes)).ToList(),
                Namespace = xElement.Name.NamespaceName
            };

            SafeName(@class, @classes);

            if (xElement.Parent == null || (!@classes.Any(ci => ci.Name.Equals(@class.Name) && ci.XmlName.Equals(@class.XmlName)) && @class.Fields.Any()))
                @classes.Add(@class);
            else
                Debug.WriteLine(@class.Name + "\n" + @class.XmlName);

            return @class;

        }

        private static string GetCompleteName(XElement xElement, XElement xParentElement)
        {
            if (xParentElement == null)
                return xElement.Name.LocalName;

            return GetCompleteName(xParentElement, xParentElement.Parent) + xElement.Name.LocalName;

        }

        private static IEnumerable<Field> ExtractFields(XElement xElement, ICollection<Class> classes)
        {
            foreach (var element in xElement.Elements().ToList())
            {
                var tempClass = ElementToClass(element, classes);
                var type = element.IsEmpty() ? "String" : tempClass.Name;

                yield return new Field
                {
                    Name = tempClass.Name,
                    Type = type,
                    XmlName = tempClass.XmlName,
                    XmlType = XmlType.Element,
                    Namespace = tempClass.Namespace
                };
            }

            foreach (var attribute in xElement.Attributes().ToList())
            {
                yield return new Field
                {
                    Name = attribute.Name.LocalName,
                    XmlName = attribute.Name.LocalName,
                    Type = attribute.Value.GetType().Name,
                    XmlType = XmlType.Attribute,
                    Namespace = attribute.Name.NamespaceName
                };
            }
        }

        private static IEnumerable<Field> ReplaceDuplicatesWithLists(IEnumerable<Field> fields)
        {
            return fields.GroupBy(field => field.Name, field => field,
                (key, g) =>
                    g.Count() > 1
                        ? new Field()
                        {
                            Name = key,
                            Namespace = g.First().Namespace,
                            Type = string.Format("List<{0}>", g.First().Type),
                            XmlName = g.First().Type,
                            XmlType = XmlType.Element
                        } :
                        g.First()).ToList();
        }

        private static void SafeName(Class @class, IEnumerable<Class> classes)
        {
            var count = classes.Count(c => c.XmlName == @class.Name);
            if (count > 0 && !@classes.Contains(@class))
            {
                @class.Name = StripBadCharacters(@class) + (count + 1);
            }
            else
            {
                @class.Name = StripBadCharacters(@class);
            }
        }

        private static string StripBadCharacters(Class @class)
        {
            return @class.Name.Replace("-", "");
        }
    }
}