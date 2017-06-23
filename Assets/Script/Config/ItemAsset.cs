using UnityEngine;
using System;
using System.Collections.Generic;
namespace QiYuan.Config
{
	
	[Serializable]
	public class ItemAssetProperty
	{
		[SerializeField]private int mid;
		[SerializeField]private string mremark;
		[SerializeField]private string mdesc;
		[SerializeField]private string mname;
		[SerializeField]private string micon;
		[SerializeField]private int miconLevel;
		[SerializeField]private int mquality;
		[SerializeField]private int mtype;
		[SerializeField]private int mstype;
		[SerializeField]private int msstype;
		[SerializeField]private int[] mbutton;
		[SerializeField]private int myingxiang;
		[SerializeField]private string mattr;
		[SerializeField]private int mlearnSpell;
		[SerializeField]private int mnextbook;
		[SerializeField]private int moverlap;
		[SerializeField]private int mneed_level;
		[SerializeField]private int mcan_sell;
		[SerializeField]private int msell_price;
		[SerializeField]private int mcan_turn;
		[SerializeField]private int mturn_price;
		[SerializeField]private int mcompose_ID;
		[SerializeField]private int mcompose_price;
		public int id
		{
			get{ return mid; }
			set{ mid = value; }
		}
		public string remark
		{
			get{ return mremark; }
			set{ mremark = value; }
		}
		public string desc
		{
			get{ return mdesc; }
			set{ mdesc = value; }
		}
		public string name
		{
			get{ return mname; }
			set{ mname = value; }
		}
		public string icon
		{
			get{ return micon; }
			set{ micon = value; }
		}
		public int iconLevel
		{
			get{ return miconLevel; }
			set{ miconLevel = value; }
		}
		public int quality
		{
			get{ return mquality; }
			set{ mquality = value; }
		}
		public int type
		{
			get{ return mtype; }
			set{ mtype = value; }
		}
		public int stype
		{
			get{ return mstype; }
			set{ mstype = value; }
		}
		public int sstype
		{
			get{ return msstype; }
			set{ msstype = value; }
		}
		public int[] button
		{
			get{ return mbutton; }
			set{ mbutton = value; }
		}
		public int yingxiang
		{
			get{ return myingxiang; }
			set{ myingxiang = value; }
		}
		public string attr
		{
			get{ return mattr; }
			set{ mattr = value; }
		}
		public int learnSpell
		{
			get{ return mlearnSpell; }
			set{ mlearnSpell = value; }
		}
		public int nextbook
		{
			get{ return mnextbook; }
			set{ mnextbook = value; }
		}
		public int overlap
		{
			get{ return moverlap; }
			set{ moverlap = value; }
		}
		public int need_level
		{
			get{ return mneed_level; }
			set{ mneed_level = value; }
		}
		public int can_sell
		{
			get{ return mcan_sell; }
			set{ mcan_sell = value; }
		}
		public int sell_price
		{
			get{ return msell_price; }
			set{ msell_price = value; }
		}
		public int can_turn
		{
			get{ return mcan_turn; }
			set{ mcan_turn = value; }
		}
		public int turn_price
		{
			get{ return mturn_price; }
			set{ mturn_price = value; }
		}
		public int compose_ID
		{
			get{ return mcompose_ID; }
			set{ mcompose_ID = value; }
		}
		public int compose_price
		{
			get{ return mcompose_price; }
			set{ mcompose_price = value; }
		}
	}
	public class ItemAsset : ConfigAssetBase<ItemAssetProperty>
	{
		private static string Path = "ConfigAssets/ItemAsset";
	}
}
