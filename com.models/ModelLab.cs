using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    public enum ControlType
    {
        [StringValue("TextBox")]
        TextBox,
        [StringValue("TextArea")]
        TextArea,
        [StringValue("ListBoxMulti")]
        ListBoxMulti,
        [StringValue("TextBoxDateTime")]
        TextBoxDateTime,
        [StringValue("TextBoxDDL")]
        TextBoxDDL

    }
    public class IdNameValue
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class StringValueAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string StringValue { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public StringValueAttribute(string value)
        {
            this.StringValue = value;
        }

        #endregion
    }

    [Alias("EASY_PUMP_EXTERNAL_ENTRY_MAP")]
    public class ExternalEntryMap : UniqueFilterable
    {
        public int ID { get; set; }
        public string Entry_Code { get; set; }
        public int Engine_ID { get; set; }
        public string Memo { get; set; }

        public object GetId()
        {
            return Entry_Code;
        }
    }
}
