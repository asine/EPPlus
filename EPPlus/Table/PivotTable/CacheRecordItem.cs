﻿/*******************************************************************************
* You may amend and distribute as you like, but don't remove this header!
*
* EPPlus provides server-side generation of Excel 2007/2010 spreadsheets.
* See http://www.codeplex.com/EPPlus for details.
*
* Copyright (C) 2011-2018 Jan Källman, Evan Schallerer, and others as noted in the source history.
*
* This library is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 2.1 of the License, or (at your option) any later version.
* This library is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
* See the GNU Lesser General Public License for more details.
*
* The GNU Lesser General Public License can be viewed at http://www.opensource.org/licenses/lgpl-license.php
* If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
*
* All code and executables are provided "as is" with no warranty either express or implied. 
* The author accepts no liability for any damage or loss of business that this product may cause.
*
* For code change notes, see the source control history.
*******************************************************************************/
using System;
using System.Xml;
using OfficeOpenXml.Extensions;
using OfficeOpenXml.Utils;

namespace OfficeOpenXml.Table.PivotTable
{
	/// <summary>
	/// Wraps a node in <pivotCacheRecords-r/>.
	/// </summary>
	public class CacheRecordItem
	{
		#region Properties
		/// <summary>
		/// Gets or sets the type of this item.
		/// </summary>
		public PivotCacheRecordType Type { get; private set; }

		/// <summary>
		/// Gets or sets the value of this item.
		/// </summary>
		public string Value
		{
			get { return this.Node.Attributes["v"]?.Value; }
			private set { this.Node.Attributes["v"].Value = value; }
		}

		private XmlNode Node { get; set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates an instance of a <see cref="CacheRecordItem"/>.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> for this <see cref="CacheRecordItem"/>.</param>
		public CacheRecordItem(XmlNode node)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			this.Node = node;
			this.Type = (PivotCacheRecordType)Enum.Parse(typeof(PivotCacheRecordType), node.Name);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Update the value of this <see cref="CacheRecordItem"/>.
		/// </summary>
		/// <param name="value">The update value.</param>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="cacheField">The cache field.</param>
		public void UpdateValue(object value, XmlNode parentNode, CacheFieldNode cacheField)
		{
			if (parentNode == null)
				throw new ArgumentNullException(nameof(parentNode));
			if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
			{
				// Match values with shared strings.
				foreach (var item in cacheField.Items)
				{
					if (stringValue.IsEquivalentTo(item.Value))
					{
						if (this.Type == PivotCacheRecordType.x)
						{
							var index = int.Parse(this.Value);
							if (cacheField.Items[index].Value != stringValue)
								this.Value = cacheField.GetSharedItemIndex(stringValue).ToString();
						}
						else
						{
							this.ReplaceNode(PivotCacheRecordType.x, parentNode);
							this.Value = cacheField.GetSharedItemIndex(stringValue).ToString();
						}
						return;
					}
				}
				if (this.Type != PivotCacheRecordType.x)
					this.ReplaceNode(PivotCacheRecordType.x, parentNode);
				this.Value = cacheField.AddItem(stringValue).ToString();
			}
			else
			{
				if (!this.Value.IsEquivalentTo(value?.ToString()))
				{
					var type = this.GetObjectType(value);
					if (this.Type != type)
						this.ReplaceNode(type, parentNode);
					if (type != PivotCacheRecordType.m)
						this.Value = value.ToString();
				}
			}
		}
		#endregion

		#region Private Methods
		private PivotCacheRecordType GetObjectType(object value)
		{
			if (value is bool)
				return PivotCacheRecordType.b;
			else if (value is DateTime)
				return PivotCacheRecordType.d;
			else if (value is ExcelErrorValue)
				return PivotCacheRecordType.e;
			else if (value == null || (value is string stringValue && string.IsNullOrEmpty(stringValue)))
				return PivotCacheRecordType.m;
			else if (ConvertUtil.IsNumeric(value, true))
				return PivotCacheRecordType.n;
			else if (value is string)
				return PivotCacheRecordType.s;
			else
				throw new InvalidOperationException($"Unknown type of {value.GetType()}.");
		}

		private void ReplaceNode(PivotCacheRecordType type, XmlNode parentNode)
		{
			var newNode = parentNode.OwnerDocument.CreateNode(XmlNodeType.Element, type.ToString(), parentNode.NamespaceURI);
			if (type != PivotCacheRecordType.m)
			{
				var attr = parentNode.OwnerDocument.CreateAttribute("v");
				newNode.Attributes.Append(attr);
			}
			parentNode.ReplaceChild(newNode, this.Node);
			this.Node = newNode;
			this.Type = type;
		}
		#endregion
	}
}