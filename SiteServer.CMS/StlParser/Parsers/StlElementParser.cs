﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using SiteServer.Utils;
using SiteServer.CMS.Plugin;
using SiteServer.CMS.Plugin.Model;
using SiteServer.CMS.StlParser.Model;
using SiteServer.CMS.StlParser.StlElement;
using SiteServer.CMS.StlParser.Utility;

namespace SiteServer.CMS.StlParser.Parsers
{
    /// <summary>
    /// Stl元素解析器
    /// </summary>
    public class StlElementParser
    {
        private StlElementParser()
        {
        }

        /// <summary>
        /// 将原始内容中的STL元素替换为实际内容
        /// </summary>
        public static void ReplaceStlElements(StringBuilder parsedBuilder, PageInfo pageInfo, ContextInfo contextInfo)
        {
            var stlElements = StlParserUtility.GetStlElementList(parsedBuilder.ToString());
            foreach (var stlElement in stlElements)
            {
                try
                {
                    var startIndex = parsedBuilder.ToString().IndexOf(stlElement, StringComparison.Ordinal);
                    if (startIndex == -1) continue;

                    var parsedContent = ParseStlElement(stlElement, pageInfo, contextInfo);
                    parsedBuilder.Replace(stlElement, parsedContent, startIndex, stlElement.Length);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static readonly Dictionary<string, Func<PageInfo, ContextInfo, string>> ElementsToParseDic = new Dictionary<string, Func<PageInfo, ContextInfo, string>>
        {
            {StlA.ElementName.ToLower(), StlA.Parse},
            {StlAction.ElementName.ToLower(), StlAction.Parse},
            {StlAudio.ElementName.ToLower(), StlAudio.Parse},
            {StlChannel.ElementName.ToLower(), StlChannel.Parse},
            {StlChannels.ElementName.ToLower(), StlChannels.Parse},
            {StlCode.ElementName.ToLower(), StlCode.Parse},
            {StlContainer.ElementName.ToLower(), StlContainer.Parse},
            {StlContent.ElementName.ToLower(), StlContent.Parse},
            {StlContents.ElementName.ToLower(), StlContents.Parse},
            {StlCount.ElementName.ToLower(), StlCount.Parse},
            {StlDynamic.ElementName.ToLower(), StlDynamic.Parse},
            {StlEach.ElementName.ToLower(), StlEach.Parse},
            {StlFile.ElementName.ToLower(), StlFile.Parse},
            {StlFlash.ElementName.ToLower(), StlFlash.Parse},
            {StlFocusViewer.ElementName.ToLower(), StlFocusViewer.Parse},
            {StlIf.ElementName.ToLower(), StlIf.Parse},
            {StlImage.ElementName.ToLower(), StlImage.Parse},
            {StlInclude.ElementName.ToLower(), StlInclude.Parse},
            {StlLocation.ElementName.ToLower(), StlLocation.Parse},
            {StlMarquee.ElementName.ToLower(), StlMarquee.Parse},
            {StlNavigation.ElementName.ToLower(), StlNavigation.Parse},
            {StlPlayer.ElementName.ToLower(), StlPlayer.Parse},
            {StlPrinter.ElementName.ToLower(), StlPrinter.Parse},
            {StlRss.ElementName.ToLower(), StlRss.Parse},
            {StlSearch.ElementName.ToLower(), StlSearch.Parse},
            {StlSearch.ElementName2.ToLower(), StlSearch.Parse},
            {StlSelect.ElementName.ToLower(), StlSelect.Parse},
            {StlSite.ElementName.ToLower(), StlSite.Parse},
            {StlSites.ElementName.ToLower(), StlSites.Parse},
            {StlSqlContent.ElementName.ToLower(), StlSqlContent.Parse},
            {StlSqlContents.ElementName.ToLower(), StlSqlContents.Parse},
            {StlTabs.ElementName.ToLower(), StlTabs.Parse},
            {StlTags.ElementName.ToLower(), StlTags.Parse},
            {StlTree.ElementName.ToLower(), StlTree.Parse},
            {StlValue.ElementName.ToLower(), StlValue.Parse},
            {StlVideo.ElementName.ToLower(), StlVideo.Parse},
            {StlZoom.ElementName.ToLower(), StlZoom.Parse}
        };

        private static readonly Dictionary<string, Func<string, string>> ElementsToTranslateDic = new Dictionary<string, Func<string, string>>
        {
            {StlPageContents.ElementName.ToLower(), StlParserManager.StlEncrypt},
            {StlPageChannels.ElementName.ToLower(), StlParserManager.StlEncrypt},
            {StlPageSqlContents.ElementName.ToLower(), StlParserManager.StlEncrypt},
            //{StlPageInputContents.ElementName.ToLower(), StlParserManager.StlEncrypt},
            {StlPageItems.ElementName.ToLower(), StlParserManager.StlEncrypt}
        };

        internal static string ParseStlElement(string stlElement, PageInfo pageInfo, ContextInfo contextInfo)
        {
            string parsedContent = null;
            //var parsedContent = StlCacheManager.ParsedContent.GetParsedContent(stlElement, pageInfo, contextInfo);
            //if (parsedContent != null) return parsedContent;

            var xmlDocument = StlParserUtility.GetXmlDocument(stlElement, contextInfo.IsInnerElement);
            XmlNode node = xmlDocument.DocumentElement;
            if (node != null)
            {
                node = node.FirstChild;

                if (node?.Name != null)
                {
                    var elementName = node.Name.ToLower();

                    if (ElementsToTranslateDic.ContainsKey(elementName))
                    {
                        Func<string, string> func;
                        if (ElementsToTranslateDic.TryGetValue(elementName, out func))
                        {
                            parsedContent = func(stlElement);
                        }
                    }
                    else if (ElementsToParseDic.ContainsKey(elementName))
                    {
                        var isDynamic = false;
                        var attributes = new Dictionary<string, string>();
                        var innerXml = StringUtils.Trim(node.InnerXml);
                        var childNodes = node.ChildNodes;

                        var ie = node.Attributes?.GetEnumerator();
                        if (ie != null)
                        {
                            while (ie.MoveNext())
                            {
                                var attr = (XmlAttribute) ie.Current;

                                if (StringUtils.EqualsIgnoreCase(attr.Name, "isDynamic"))
                                {
                                    isDynamic = TranslateUtils.ToBool(attr.Value, false);
                                }
                                else
                                {
                                    var key = attr.Name;
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        var value = attr.Value;
                                        if (string.IsNullOrEmpty(StringUtils.Trim(value)))
                                        {
                                            value = string.Empty;
                                        }
                                        attributes[key] = value;
                                    }
                                }
                            }
                        }

                        if (isDynamic)
                        {
                            parsedContent = StlDynamic.ParseDynamicElement(stlElement, pageInfo, contextInfo);
                        }
                        else
                        {
                            try
                            {
                                Func<PageInfo, ContextInfo, string> func;
                                if (ElementsToParseDic.TryGetValue(elementName, out func))
                                {
                                    parsedContent = func(pageInfo,
                                        contextInfo.Clone(stlElement, attributes, innerXml, childNodes));
                                }
                            }
                            catch (Exception ex)
                            {
                                parsedContent = StlParserUtility.GetStlErrorMessage(elementName, stlElement, ex);
                            }
                        }
                    }
                    else
                    {
                        var parsers = PluginStlParserContentManager.GetParses();
                        if (parsers.ContainsKey(elementName))
                        {
                            var isDynamic = false;
                            var attributes = new Dictionary<string, string>();
                            var innerXml = StringUtils.Trim(node.InnerXml);

                            var ie = node.Attributes?.GetEnumerator();
                            if (ie != null)
                            {
                                while (ie.MoveNext())
                                {
                                    var attr = (XmlAttribute)ie.Current;

                                    if (StringUtils.EqualsIgnoreCase(attr.Name, "isDynamic"))
                                    {
                                        isDynamic = TranslateUtils.ToBool(attr.Value, false);
                                    }
                                    else
                                    {
                                        var key = attr.Name;
                                        if (!string.IsNullOrEmpty(key))
                                        {
                                            var value = attr.Value;
                                            if (string.IsNullOrEmpty(StringUtils.Trim(value)))
                                            {
                                                value = string.Empty;
                                            }
                                            attributes[key] = value;
                                        }
                                    }
                                }
                            }

                            if (isDynamic)
                            {
                                parsedContent = StlDynamic.ParseDynamicElement(stlElement, pageInfo, contextInfo);
                            }
                            else
                            {
                                try
                                {
                                    Func<PluginParseContext, string> func;
                                    if (parsers.TryGetValue(elementName, out func))
                                    {
                                        var context = new PluginParseContext(attributes, innerXml, pageInfo, contextInfo);
                                        parsedContent = func(context);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    parsedContent = StlParserUtility.GetStlErrorMessage(elementName, stlElement, ex);
                                }
                            }
                        }
                    }
                }
            }

            if (parsedContent == null)
            {
                parsedContent = stlElement;
            }
            else
            {
                parsedContent = contextInfo.IsInnerElement ? parsedContent : StlParserUtility.GetBackHtml(parsedContent, pageInfo);
            }

            //StlCacheManager.ParsedContent.SetParsedContent(stlElement, pageInfo, contextInfo, parsedContent);
            return parsedContent;
        }
    }
}
