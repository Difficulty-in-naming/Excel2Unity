using UnityEngine;
using System;
using System.Collections.Generic;
namespace QiYuan.Config
{
	[Serializable]
	public class Attribute2
	{
		[SerializeField]private int[] mId;
		[SerializeField]private int mValue;
		[SerializeField]private string[] mS;
		public int[] Id
		{
			get{ return mId; }
			set{ mId = value; }
		}
		public int Value
		{
			get{ return mValue; }
			set{ mValue = value; }
		}
		public string[] S
		{
			get{ return mS; }
			set{ mS = value; }
		}
	}
	[Serializable]
	public class Attribute
	{
		[SerializeField]private int mId;
		[SerializeField]private int mValue;
		public int Id
		{
			get{ return mId; }
			set{ mId = value; }
		}
		public int Value
		{
			get{ return mValue; }
			set{ mValue = value; }
		}
	}
	[Serializable]
	public class EquipmentAssetsProperty
	{
		[SerializeField]private int mId;
		[SerializeField]private string mName;
		[SerializeField]private string mRemark;
		[SerializeField]private int[] mPrice;
		[SerializeField]private Attribute mAttribute;
		[SerializeField]private Attribute2 mAttribute2;
		public int Id
		{
			get{ return mId; }
			set{ mId = value; }
		}
		public string Name
		{
			get{ return mName; }
			set{ mName = value; }
		}
		public string Remark
		{
			get{ return mRemark; }
			set{ mRemark = value; }
		}
		public int[] Price
		{
			get{ return mPrice; }
			set{ mPrice = value; }
		}
		public Attribute Attribute
		{
			get{ return mAttribute; }
			set{ mAttribute = value; }
		}
		public Attribute2 Attribute2
		{
			get{ return mAttribute2; }
			set{ mAttribute2 = value; }
		}
	}
	public class EquipmentAssets : ConfigAssetBase<EquipmentAssetsProperty>
	{
		private static string Path = "ConfigAssets/EquipmentAssets";
	}
}
