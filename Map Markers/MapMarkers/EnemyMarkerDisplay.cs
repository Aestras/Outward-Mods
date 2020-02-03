﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MapMarkers
{
	public class EnemyMarkerDisplay : MapWorldMarkerDisplay, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IEventSystemHandler
	{
		public new Text Text;
		public new RectTransform Holder;
		public new Image Background;
		public new Image Circle;

		private bool m_hover;

		protected override void StartInit()
		{
			//base.StartInit();
			//this.Circle.transform.SetParent(base.transform.parent);
			//this.Circle.transform.SetAsFirstSibling();
			//this.Circle.gameObject.SetActive(base.gameObject.activeSelf);
		}

		public new void SetActive(bool _active)
		{
			base.gameObject.SetActive(_active);
			//this.Circle.gameObject.SetActive(_active);
		}

		public void UpdateDisplay(EnemyMarker _marker)
		{
			//if (this.Text.text != _marker.Text)
			//{
			//	this.Text.text = _marker.Text;
			//}
			if (m_hover)
			{
				if (this.Text.text != _marker.Text)
				{
					this.Text.text = _marker.Text;
				}
			}
			else
			{
				string s = _marker.LinkedCharacter.ActiveMaxHealth >= 500 ? "X" : "x";
				if (this.Text.text != s)
				{
					this.Text.text = s;
				}
			}

			_marker.MarkerWidth = this.Holder.rect.width;

			base.RectTransform.localPosition = _marker.AdjustedMapPosition;

			if (_marker.ShowBackground)
			{
				if (!this.Background.gameObject.activeSelf)
				{
					this.Background.gameObject.SetActive(true);
				}
			}
			else if (this.Background.gameObject.activeSelf)
			{
				this.Background.gameObject.SetActive(false);
			}

			if (!_marker.AlignLeft)
			{
				base.RectTransform.pivot = new Vector2(0f, 0.5f);
				this.Holder.pivot = new Vector2(0f, 0.5f);
				this.Holder.anchoredPosition = new Vector2(12f, 0f);
				this.Background.rectTransform.localScale = new Vector3(1f, 0.85f, 1f);
				this.Background.rectTransform.offsetMin = new Vector2(-2f, 0f);
				this.Background.rectTransform.offsetMax = new Vector2(26.48f, 0f);
				this.Background.color = new Color(1f, 1f, 1f, 0.9f);
			}
			else
			{
				base.RectTransform.pivot = new Vector2(0f, 0.5f);
				this.Holder.pivot = new Vector2(1f, 0.5f);
				this.Holder.anchoredPosition = new Vector2(-12f, 0f);
				this.Background.rectTransform.localScale = new Vector3(-1f, 0.85f, 1f);
				this.Background.rectTransform.offsetMin = new Vector2(-21f, 0f);
				this.Background.rectTransform.offsetMax = new Vector2(2f, 0f);
				this.Background.color = new Color(1f, 1f, 1f, 0.9f);
			}
		}

		public void OnPointerEnter(PointerEventData _eventData)
		{
			this.m_hover = true;
			//MapDisplay.Instance.HoveredMarker = this;
		}

		public void OnPointerExit(PointerEventData _eventData)
		{
			this.m_hover = false;
		}

		public void OnPointerClick(PointerEventData _eventData)
		{
			// could do something with this?
		}
	}
}
