using System;

namespace IFY.Phorm.EventArgs
{
    public class UnexpectedRecordColumnEventArgs : System.EventArgs
    {
        /// <summary>
        /// The unique GUID for the command instance that raised this event.
        /// </summary>
        public Guid CommandGuid { get; internal set; }

        /// <summary>
        /// The type of entity being constructed.
        /// </summary>
        public Type EntityType { get; internal set; } = null!;

        /// <summary>
        /// The name of the record column that cannot be mapped to the entity.
        /// </summary>
        public string ColumnName { get; internal set; } = string.Empty;
    }
}
