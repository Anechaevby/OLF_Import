using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Export_Console.Lib
{
    // ReSharper disable InconsistentNaming
    public enum ApplicationStateEnum {unknown, draft, ready_to_sign, ready_to_send, sent}
    public enum PmsModeEnum {demo, production}
    public enum PmsActionEnum {none, validate, import, sign, send, export, remove, information}
// ReSharper restore InconsistentNaming
    internal enum LanguageTypeEnum {English, German, French, Spanish};

    internal static class XmlUtils
    {
        internal static XmlNode FirstChildByName(XmlNode node, string name)
        {
            IEnumerator ienum = node.GetEnumerator();
            while (ienum.MoveNext())
            {
                var tmpNode = (XmlNode) ienum.Current;
                if (tmpNode.LocalName == name)
                {
                    return tmpNode;
                }
            }
            return null;
        }

        internal static string FirstChildByNameValue(XmlNode node, string name, string defValue)
        {
            XmlNode chlNode = FirstChildByName(node, name);
            if (chlNode == null)
            {
                return defValue;
            }
            StringBuilder stringBuilder = new StringBuilder();
            IEnumerator ienum = node.GetEnumerator();
            while (ienum.MoveNext())
            {
                var text = ienum.Current as XmlText;
                if (text != null)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(" ");
                    }
                    stringBuilder.Append(text.Value);
                }
            }
            return stringBuilder.ToString();
        }

        internal static XmlElement AddChild(XmlNode node, string name, string value)
        {
            if (node.OwnerDocument != null)
            {
                XmlElement childTag = node.OwnerDocument.CreateElement(name);
                node.AppendChild(childTag);
                if (!string.IsNullOrEmpty(value))
                {
                    if (node.OwnerDocument != null)
                    {
                        XmlText text = node.OwnerDocument.CreateTextNode(value);
                        childTag.AppendChild(text);
                    }
                }
                return childTag;
            }
            return null;
        }
    }

    internal abstract class PmsAction
    {
        internal readonly Dictionary<string, string> OfficeParamsList = new Dictionary<string, string>();
        internal int ApplicationId = -1;
        private string _appProcedure; 
        internal string ApplicationProcedure
        {
            get => _appProcedure;
            set
            {
                switch (value)
                {
                    case "PCT":
                        _appProcedure = "IBR101";
                        break;
                    case "EP1001":
                        _appProcedure = "EP2000";
                        break;
                    case "EP1200":
                        _appProcedure = "EP122K";
                        break;
                    default:
                        _appProcedure = value;
                        break;
                }
            }
        }
        internal string Folder = string.Empty;
        internal ApplicationStateEnum ApplicationState = ApplicationStateEnum.unknown;
        internal string OfficeCode = string.Empty;
        internal bool FinalSignature;
        internal bool FacsimileSign;
        internal string FacsimileFileName = string.Empty;
        internal string SignXml = string.Empty;
        internal ApplicationStateEnum ImportState;
        internal bool PdfPreview;
        internal abstract PmsActionEnum ActionType();
        internal string ActionName()
        {
            return Enum.GetName(typeof(PmsActionEnum), ActionType()); 
        }
        internal virtual void Parse(XmlElement xmlTag)
        {
            XmlElement paramsTag = XmlUtils.FirstChildByName(xmlTag, "office_specific_parameters") as XmlElement;
            if (paramsTag != null )
            {
                OfficeCode = paramsTag.GetAttribute("office_code");

                IEnumerator ienum = paramsTag.GetEnumerator();
                while (ienum.MoveNext())
                {
                    if (ienum is XmlElement)
                    {
                        var paramTag = (XmlElement)ienum.Current;
                        if (paramTag.Name == "office_specific_parameter")
                        {
                            OfficeParamsList.Add(paramTag.GetAttribute("name"), paramTag.GetAttribute("value"));
                        }
                    }
                }
            }
        }

        internal virtual void BuildXml(XmlElement xmlTag)
        {
            if (OfficeParamsList.Any())
            {
                XmlElement paramsTag = xmlTag.OwnerDocument?.CreateElement("office_specific_parameters");

                if (paramsTag != null)
                {
                    xmlTag.AppendChild(paramsTag);
                    paramsTag.SetAttribute("office_code", OfficeCode);
                    foreach (KeyValuePair<string, string > keyValuePair in OfficeParamsList)
                    {
                        var paramTag = paramsTag.OwnerDocument?.CreateElement("office_specific_parameter");
                        if (paramTag != null)
                        {
                            paramsTag.AppendChild(paramTag);
                            paramTag.SetAttribute("name", keyValuePair.Key);
                            paramTag.SetAttribute("value", keyValuePair.Value);
                        }
                    }
                }
            }
        }
    }

    internal class PmsValidateAction: PmsAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.validate;
        }
        internal override void Parse(XmlElement xmlTag)
        {
            PdfPreview = xmlTag.GetAttribute("pdf_preview").Equals("yes", StringComparison.CurrentCultureIgnoreCase);
            base.Parse(xmlTag);
        }

        internal override void BuildXml(XmlElement xmlTag)
        {
            xmlTag.SetAttribute("pdf_preview", PdfPreview ? "yes" : "no");
            base.BuildXml(xmlTag);
        }
    }

    internal class PmsImportAction: PmsAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.import;
        }
        internal override void Parse(XmlElement xmlTag)
        {
            try
            {
                ImportState = (ApplicationStateEnum) Enum.Parse(typeof (ApplicationStateEnum), xmlTag.GetAttribute("import_state"));
            }
            catch (ArgumentException)
            {
                ImportState = ApplicationStateEnum.unknown;
            }
            Folder = XmlUtils.FirstChildByNameValue(xmlTag, "folder", string.Empty);
            base.Parse(xmlTag);
        }
        
        internal override void BuildXml(XmlElement xmlTag)
        {
            xmlTag.SetAttribute("import_state", Enum.GetName(typeof (ApplicationStateEnum), ImportState));
            XmlUtils.AddChild(xmlTag, "folder", Folder);
            base.BuildXml(xmlTag);
        }
    }

    internal class PmsSendAction: PmsAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.send;
        }
        internal override void Parse(XmlElement xmlTag)
        {
            if (!int.TryParse(XmlUtils.FirstChildByNameValue(xmlTag, "application_id", "-1"), out ApplicationId))
            {
                ApplicationId = -1; 
            }
        }
        
        internal override void BuildXml(XmlElement xmlTag)
        {
            XmlUtils.AddChild(xmlTag, "application_id", ApplicationId.ToString());
        }
    }

    internal abstract class PmsFilterAction: PmsAction
    {
        internal override void Parse(XmlElement xmlTag)
        {
            if (!Int32.TryParse(XmlUtils.FirstChildByNameValue(xmlTag, "application_id", "-1"), out ApplicationId))
            {
                ApplicationId = -1;    
            }
            if (ApplicationId > -1)
            {
                Folder = XmlUtils.FirstChildByNameValue(xmlTag, "folder", "");
                ApplicationProcedure = XmlUtils.FirstChildByNameValue(xmlTag, "procedure", string.Empty);
                XmlNode stateTag = XmlUtils.FirstChildByName(xmlTag, "application_state");
                ApplicationState = ApplicationStateEnum.unknown;

                if (stateTag != null && stateTag.ChildNodes.Count == 1)
                {
                    try
                    {
                        ApplicationState = (ApplicationStateEnum) Enum.Parse(typeof (ApplicationStateEnum), stateTag.ChildNodes[0].LocalName);
                    }
                    catch (ArgumentException)
                    {
                        ApplicationState = ApplicationStateEnum.unknown;
                    }
                }

            }
        }
        
        internal override void BuildXml(XmlElement xmlTag)
        {
            if (ApplicationId > -1)
            {
                XmlUtils.AddChild(xmlTag, "application_id", ApplicationId.ToString());
            }
            else
            {
                if (!string.IsNullOrEmpty(Folder))
                {
                    XmlUtils.AddChild(xmlTag, "folder", Folder);
                }
                if (ApplicationState > ApplicationStateEnum.unknown)
                {
                    XmlUtils.AddChild(XmlUtils.AddChild(xmlTag, "application_state", string.Empty),
                                      Enum.GetName(typeof (ApplicationStateEnum), ApplicationState), string.Empty);
                }
                if (!string.IsNullOrEmpty(ApplicationProcedure))
                {
                    XmlUtils.AddChild(xmlTag, "procedure", ApplicationProcedure);
                }
            }
        }
    }
  
    class PmsExportAction: PmsFilterAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.export;
        }
    }

    class PmsRemoveAction: PmsFilterAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.remove;
        }
    }
 
    class PmsInformationAction: PmsFilterAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.information;
        }
    }

    class PmsSignAction: PmsFilterAction
    {
        internal override PmsActionEnum ActionType()
        {
            return PmsActionEnum.sign;
        }
        internal override void Parse(XmlElement xmlTag)
        {
            if (!Int32.TryParse(XmlUtils.FirstChildByNameValue(xmlTag, "application_id", "-1"), out ApplicationId))
            {
                ApplicationId = -1;
            }
            FinalSignature = xmlTag.GetAttribute("final_signature") == "yes";
            XmlNode signatoryTag = XmlUtils.FirstChildByName(xmlTag, "signatories");
            if (signatoryTag != null)
            {
                SignXml = signatoryTag.InnerXml;
                if (signatoryTag.ChildNodes.Count != 1 && !signatoryTag.ChildNodes[0].LocalName.Equals("signatory"))
                {
                    throw new EpmsException("Signature record is incorrect. There must be only one signature record.");
                }
                XmlNode signatureTag = signatoryTag.ChildNodes[0];
                signatureTag = XmlUtils.FirstChildByName(signatureTag, "electronic-signature");
                if (signatureTag != null)
                {
                    signatureTag = XmlUtils.FirstChildByName(signatureTag, "basic-signature");
                    if (signatureTag != null)
                    {
                        XmlElement imageTag = XmlUtils.FirstChildByName(signatureTag, "fax-image") as XmlElement;
                        FacsimileSign = imageTag != null;
                        if (imageTag != null)
                        {
                            FacsimileFileName = imageTag.GetAttribute("file");                                                    
                        }
                    }
                }
            }
        }
        internal override void BuildXml(XmlElement xmlTag)
        {
            xmlTag.SetAttribute("final_signature", FinalSignature ? "yes" : "no");
            XmlUtils.AddChild(xmlTag, "application_id", ApplicationId.ToString());
            xmlTag.InnerXml = xmlTag.InnerXml + SignXml;
        }
    }

    [Serializable]
    public class EpmsException : Exception
    {
        public EpmsException(string message): base(message)
        {
        }
        protected EpmsException(SerializationInfo serializationInfo, StreamingContext streamingContext): 
            base(serializationInfo, streamingContext)
        {
        }
        public EpmsException()
        {
        }
        public EpmsException(String message, Exception ex): base (message, ex)
        {
        }
        
    }

    internal class PmsInput
    {
        internal string Password = String.Empty;
        internal string UserName = String.Empty;
        internal LanguageTypeEnum Language;
        internal PmsModeEnum Mode;
        internal string InputDTDPath = String.Empty;
        internal PmsAction Action { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        internal string XmlString
        {
            get
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlElement root = xmlDocument.CreateElement("PMS_2_OLF_input");
                xmlDocument.AppendChild(root);
                root.SetAttribute("user_name", UserName);
                root.SetAttribute("password", Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)));
                root.SetAttribute("mode", Enum.GetName(typeof(PmsModeEnum), Mode));
                root.SetAttribute("language", LanguageToCode(Language));
                if (Action != null)
                {
                    XmlElement xmlElement = XmlUtils.AddChild(root, Action.ActionName(), String.Empty);
                    Action.BuildXml(xmlElement);
                }
                return xmlDocument.OuterXml;
            }
            set
            {
                Clear();
                XmlDocument xmlDocument = new XmlDocument();
                try
                {
                    xmlDocument.LoadXml(value);
                }
                catch
                {
                    throw new EpmsException("Response XML is not valid");
                }
                //DeleteFile(ChangeFileExt(InputDTDPath, '.xml'));
                XmlNode root = xmlDocument.FirstChild;
                while ((root != null) && !(root is XmlElement))
                {
                    root = root.NextSibling;    
                }
                XmlElement rootElement = (XmlElement)root;
                if (rootElement != null)
                {
                    UserName = rootElement.GetAttribute("user_name");
                    Password = Encoding.UTF8.GetString(Convert.FromBase64String(rootElement.GetAttribute("password")));
                    try
                    {   
                        Mode = (PmsModeEnum) Enum.Parse(typeof (PmsModeEnum), rootElement.GetAttribute("mode"));
                    }
                    catch (ArgumentException)
                    {
                        Mode = PmsModeEnum.demo;
                    }
                    Language = CodeToLanguage(rootElement.GetAttribute("language"));
                    if (rootElement.ChildNodes.Count > 0)
                    {
                        try
                        {
                            PmsActionEnum pmsAction = (PmsActionEnum)Enum.Parse(typeof(PmsActionEnum), rootElement.ChildNodes[0].LocalName);
                            CreateActionByType(pmsAction);
                        }
                        catch (ArgumentException)
                        {
                            throw new EpmsException("Command " + rootElement.ChildNodes[0].LocalName + " is not supported");
                        }
                        Action?.Parse((XmlElement)rootElement.ChildNodes[0]);
                    }
                }
            }
        }

        private static string LanguageToCode(LanguageTypeEnum language)
        {
            switch (language)
            {
                case LanguageTypeEnum.English:
                    return "en";
                case LanguageTypeEnum.German:
                    return "de";
                case LanguageTypeEnum.French:
                    return "fr";
                case LanguageTypeEnum.Spanish:
                    return "es";
                default:
                    return "en";
            }  
        }

        internal static LanguageTypeEnum CodeToLanguage(string code)
        {
            switch (code.ToLower())
            {
                case "en":
                    return LanguageTypeEnum.English;
                case "de":
                    return LanguageTypeEnum.German;
                case "fr":
                    return LanguageTypeEnum.French;
                case "es":
                    return LanguageTypeEnum.Spanish;
                default:
                    return LanguageTypeEnum.English;
            }  
        }

        internal void Clear()
        {
            Action = null;
            Password = String.Empty;
            UserName = String.Empty;
            Language = LanguageTypeEnum.English;
            Mode = PmsModeEnum.demo;
        }
        internal void CreateActionByType(PmsActionEnum actionType)
        {
            switch (actionType)
            {
                case PmsActionEnum.validate:
                    Action = new PmsValidateAction();
                    break;
                case PmsActionEnum.import:
                    Action = new PmsImportAction();
                    break;
                case PmsActionEnum.sign:
                    Action = new PmsSignAction();
                    break;
                case PmsActionEnum.send:
                    Action = new PmsSendAction();
                    break;
                case PmsActionEnum.information:
                    Action = new PmsInformationAction();
                    break;
                case PmsActionEnum.export:
                    Action = new PmsExportAction();
                    break;
                case PmsActionEnum.remove:
                    Action = new PmsRemoveAction();
                    break;
            }
        }
   }

    
}
