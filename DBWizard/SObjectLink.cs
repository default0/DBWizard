using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Represents an Object link that connects two Object maps with a foreign key.
    /// </summary>
    internal struct SObjectLink
    {
        /// <summary>
        /// The foreign key that links the source map and the target map.
        /// </summary>
        internal CForeignKey m_p_foreign_key { get; private set; }

        /// <summary>
        /// The source map that the link refers to.
        /// </summary>
        internal CObjectMap m_p_source_map { get; private set; }

        /// <summary>
        /// The target map that the link refers to.
        /// </summary>
        internal CObjectMap m_p_target_map { get; private set; }

        /// <summary>
        /// The field that this link binds.
        /// </summary>
        internal FieldInfo m_p_field { get; private set; }
        /// <summary>
        /// The target type this object link refers to.
        /// </summary>
        public Type m_p_target_type { get; private set; }

        public EObjectLinkType m_type { get; private set; }

        /// <summary>
        /// Constructs a new Object link that connects the given source map with the given target map using the given foreign key.
        /// </summary>
        /// <param name="p_field">The field this link should bind.</param>
        /// <param name="p_foreign_key">The foreign key to link the given source and target map.</param>
        /// <param name="p_source_map">The source map that the link should refer to.</param>
        /// <param name="p_target_map">The target map that the link should refer to.</param>
        internal SObjectLink(EObjectLinkType type, FieldInfo p_field, Type p_target_type, CForeignKey p_foreign_key, CObjectMap p_source_map, CObjectMap p_target_map)
            : this()
        {
            if (p_target_type == null) throw new ArgumentNullException("The specified target type may not be null.");
            if (p_foreign_key == null) throw new ArgumentNullException("The specified foreign key may not be null.");
            if (p_source_map == null) throw new ArgumentNullException("The specified source map may not be null.");
            if (p_target_map == null) throw new ArgumentNullException("The specified target map may not be null.");

            m_type = type;
            m_p_field = p_field; // allowed to be null
            m_p_target_type = p_target_type;

            for (Int32 i = 0; i < p_foreign_key.m_p_source_columns.Count; ++i)
            {
                if (!p_source_map.m_p_primitives_map.ContainsKey(p_foreign_key.m_p_source_columns[i]))
                {
                    throw new ArgumentException("The given foreign key contains the source column \"" + p_foreign_key.m_p_source_columns[i] + "\" which is not present in the source map.");
                }
            }
            for (Int32 i = 0; i < p_foreign_key.m_p_target_columns.Count; ++i)
            {
                if (!p_target_map.m_p_primitives_map.ContainsKey(p_foreign_key.m_p_target_columns[i]))
                {
                    throw new ArgumentException("The given foreign key contains the target column \"" + p_foreign_key.m_p_target_columns[i] + "\" which is not present in the target map.");
                }
            }

            m_p_foreign_key = p_foreign_key;
            m_p_source_map = p_source_map;
            m_p_target_map = p_target_map;
        }
    }
}
