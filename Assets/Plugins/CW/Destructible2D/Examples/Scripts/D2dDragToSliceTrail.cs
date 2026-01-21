using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace Destructible2D.Examples
{
	/// <summary>This component allows you to slice all destructible sprites between the mouse down and mouse up points.</summary>
	[HelpURL(D2dCommon.HelpUrlPrefix + "D2dDragToSliceTrail")]
	[AddComponentMenu(D2dCommon.ComponentMenuPrefix + "Drag To Slice Trail")]
	public class D2dDragToSliceTrail : MonoBehaviour
	{
		class Link : CwInputManager.Link
		{
			public bool Hit;

			public Vector3 HitPosition;

			public Vector3 LastPosition;

			public TrailRenderer Visual;

			public override void Clear()
			{
				Destroy(Visual.gameObject);
			}

			public int GetFirstIndex()
			{
				if (Visual != null && Hit == true)
				{
					for (var i = 0; i < Visual.positionCount; i++)
					{
						if (Visual.GetPosition(i).Equals(HitPosition) == true)
						{
							return i;
						}
					}
				}

				return 0;
			}
		}

		public enum SliceType
		{
			OnceOnRelease,
			RaycastSlice,
			LeadingEdge
		}

		/// <summary>The controls used to trigger the slice.</summary>
		public CwInputManager.Trigger Controls = new CwInputManager.Trigger { UseFinger = true, UseMouse = true };

		/// <summary>When should the trail perform the slice?</summary>
		public SliceType Slice = SliceType.LeadingEdge;

		/// <summary>The Z position in world space this component will use. For normal 2D scenes this should be 0.</summary>
		public float Intercept;

		/// <summary>The destructible sprite layers we want to slice.</summary>
		public LayerMask Layers = -1;

		/// <summary>Should the stamp exclude a specific destructible object?</summary>
		public D2dDestructible Exclude;

		/// <summary>The prefab used to show what the slice will look like.</summary>
		public TrailRenderer IndicatorPrefab;

		/// <summary>This allows you to change the painting type.</summary>
		public D2dDestructible.PaintType Paint;

		/// <summary>The shape of the stamp.</summary>
		public D2dShape StampShape;

		/// <summary>This component stretches the <b>StampShape</b> between the slice points. This can cause edges to break or be sharp, so you can add caps to avoid this.
		/// NOTE: If you use caps, then the <b>StampShape</b> should be 1px high or stretched vertically, and the cap height should be a half the width and be rounded at the top.</summary>
		public D2dShape CapShape;

		/// <summary>The shape of the stamp when it modifies destructible RGB data.</summary>
		public Texture2D ColorShape;

		/// <summary>The shape of the stamp when it modifies destructible alpha data.</summary>
		public Texture2D AlphaShape;

		/// <summary>The stamp shape will be multiplied by this.
		/// White = No Change.</summary>
		public Color Color = Color.white;

		/// <summary>The thickness of the slice line in world space.</summary>
		public float Thickness = 1.0f;

		[SerializeField]
		private List<Link> links = new List<Link>();

		protected virtual void OnEnable()
		{
			CwInputManager.EnsureThisComponentExists();
		}

		protected virtual void Update()
		{
			// Loop through all fingers + mouse + mouse hover
			foreach (var finger in CwInputManager.GetFingers(true))
			{
				// Did this finger go down based on the current control settings?
				if (Controls.WentDown(finger) == true)
				{
					// Create a link with this finger and additional data
					var link = CwInputManager.Link.Create(ref links, finger);

					// Create an indicator for this link?
					if (IndicatorPrefab != null)
					{
						link.Visual = Instantiate(IndicatorPrefab, null, false);

						link.Visual.gameObject.SetActive(true);
					}

					link.Hit          = false;
					link.LastPosition = GetPosition(finger.ScreenPosition);
				}
			}

			// Loop through all links in reverse so they can be removed
			for (var i = links.Count - 1; i >= 0; i--)
			{
				var link     = links[i];
				var position = GetPosition(link.Finger.ScreenPosition);

				// Update indicator?
				if (link.Visual != null)
				{
					link.Visual.transform.position = position;

					if (Slice == SliceType.RaycastSlice && link.Visual != null)
					{
						var firstIndex = link.GetFirstIndex();
						var inside     = false;

						for (var j = firstIndex; j < link.Visual.positionCount - 1; j++)
						{
							var a   = link.Visual.GetPosition(j    );
							var b   = link.Visual.GetPosition(j + 1);
							var hit = Physics2D.Raycast(a, Vector3.Normalize(b - a), Vector3.Magnitude(b - a), Layers);

							if (hit.collider != null)
							{
								if (inside == false)
								{
									inside = true;
								}
							}
							else
							{
								if (inside == true)
								{
									SliceAll(link.Visual, firstIndex, j + 1);

									link.Hit         = true;
									link.HitPosition = b;

									break;
								}
							}
						}
					}

					if (Slice == SliceType.LeadingEdge && link.Visual != null)
					{
						DoSliceAll(link.LastPosition, position);
					}

					link.LastPosition = position;
				}

				// Did this finger go up based on the current control settings?
				if (Controls.WentUp(link.Finger, true) == true)
				{
					// Slice all objects in scene
					if (Slice == SliceType.OnceOnRelease && link.Visual != null)
					{
						SliceAll(link.Visual, 0, link.Visual.positionCount);
					}

					// Destroy indicator
					CwInputManager.Link.ClearAndRemove(links, link);
				}
			}
		}

		protected virtual void OnDestroy()
		{
			CwInputManager.Link.ClearAll(links);
		}

		private void SliceAll(TrailRenderer trail, int indexA, int indexB)
		{
			for (var i = indexA; i < indexB - 1; i++)
			{
				var a = trail.GetPosition(i    );
				var b = trail.GetPosition(i + 1);

				DoSliceAll(a, b);
			}
		}

		private void DoSliceAll(Vector3 a, Vector3 b)
		{
			if (CapShape.Color != null || CapShape.Alpha != null)
			{
				if (a != b)
				{
					var d = (b - a).normalized;

					D2dSlice.All(Paint, a, a - d * Thickness * 0.5f, Thickness, CapShape, Color, Layers, Exclude);
					D2dSlice.All(Paint, b, b + d * Thickness * 0.5f, Thickness, CapShape, Color, Layers, Exclude);
				}
			}

			D2dSlice.All(Paint, a, b, Thickness, StampShape, Color, Layers, Exclude);
		}

		private Vector3 GetPosition(Vector2 screenPosition)
		{
			// Make sure the camera exists
			var camera = CwHelper.GetCamera(null);

			if (camera != null)
			{
				return D2dCommon.ScreenToWorldPosition(screenPosition, Intercept, camera);
			}

			return default(Vector3);
		}
	}
}

#if UNITY_EDITOR
namespace Destructible2D.Examples
{
	using UnityEditor;
	using TARGET = D2dDragToSliceTrail;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class D2dDragToSliceTrail_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("Controls", "The controls used to trigger the stamp.");
			Draw("Slice", "When should the trail perform the slice?");
			Draw("Intercept", "The Z position in world space this component will use. For normal 2D scenes this should be 0.");
			BeginError(Any(tgts, t => t.Layers == 0));
				Draw("Layers", "The destructible sprite layers we want to slice.");
			EndError();
			Draw("Exclude", "Should the stamp exclude a specific destructible object?");
			BeginError(Any(tgts, t => t.IndicatorPrefab == null));
				Draw("IndicatorPrefab", "The prefab used to show what the slice will look like.");
			EndError();

			Separator();

			Draw("Paint", "This allows you to change the painting type.");
			Draw("StampShape", "The shape of the stamp.");
			Draw("CapShape", "This component stretches the <b>StampShape</b> between the slice points. This can cause edges to break or be sharp, so you can add caps to avoid this.\n\nNOTE: If you use caps, then the <b>StampShape</b> should be 1px high or stretched vertically, and the cap height should be a half the width and be rounded at the top.");
			Draw("Color", "The stamp shape will be multiplied by this.\n\nWhite = No Change.");
			BeginError(Any(tgts, t => t.Thickness == 0.0f));
				Draw("Thickness", "The thickness of the slice line in world space.");
			EndError();
		}
	}
}
#endif