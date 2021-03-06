﻿using System;
using System.Collections;
using System.Collections.Generic;
using HotUI.Layout;

namespace HotUI
{
	public abstract class AbstractLayout : View, IList<View>, IContainerView
    {
		readonly List<View> _views = new List<View> ();
		private readonly ILayoutManager _layout;
		
		public event EventHandler<LayoutEventArgs> ChildrenChanged;
		public event EventHandler<LayoutEventArgs> ChildrenAdded;
		public event EventHandler<LayoutEventArgs> ChildrenRemoved;

		protected AbstractLayout(ILayoutManager layoutManager)
		{
			_layout = layoutManager;
		}

		public ILayoutManager LayoutManager => _layout;
		
		public void Add (View view)
		{
			if (view == null)
				return;
			view.Parent = this;
            view.Navigation = Parent as NavigationView ?? Parent?.Navigation;
            _views.Add (view);
            _layout.Invalidate();

			ChildrenChanged?.Invoke (this, new LayoutEventArgs (_views.Count - 1, 1));
		}

		public void Clear ()
		{
			var count = _views.Count;
			if (count > 0)
            {
                var removed = new List<View>(_views);
                _views.Clear ();
                _layout.Invalidate();
				ChildrenRemoved?.Invoke (this, new LayoutEventArgs (0, count, removed));
			}
		}

		public bool Contains (View item) => _views.Contains (item);

		public void CopyTo (View [] array, int arrayIndex)
		{
			_views.CopyTo (array, arrayIndex);
			_layout.Invalidate();

			ChildrenAdded?.Invoke (this, new LayoutEventArgs (arrayIndex, array.Length));
		}

		public bool Remove (View item)
		{
			if (item == null) return false;
			
			var index = _views.IndexOf (item);
			if (index >= 0)
            {
				item.Parent = null;
                item.Navigation = null;

                var removed = new List<View> { item };
                _views.Remove (item);
                _layout.Invalidate();
				ChildrenRemoved?.Invoke (this, new LayoutEventArgs (index, 1, removed));
				return true;
			}

			return false;
		}

		public int Count => _views.Count;

		public bool IsReadOnly => false;

		public IEnumerator<View> GetEnumerator () => _views.GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator () => _views.GetEnumerator ();

		public IReadOnlyList<View> GetChildren () => _views;

		public int IndexOf (View item) => _views.IndexOf (item);

		public void Insert (int index, View item)
        {
            if (item == null)
                return;

			_views.Insert (index, item);
			_layout.Invalidate();

            item.Parent = this;
            item.Navigation = Parent as NavigationView ?? Parent?.Navigation;
            ChildrenAdded?.Invoke (this, new LayoutEventArgs (index, 1));
		}

		public void RemoveAt (int index)
        {
            if (index >= 0 && index < _views.Count)
            {
                var item = _views[index];
                item.Parent = null;
                item.Navigation = null;

                var removed = new List<View> { item };
                _views.RemoveAt(index);
                _layout.Invalidate();

                ChildrenRemoved?.Invoke(this, new LayoutEventArgs(index, 1, removed));
            }
        }

		public View this [int index]
        {
			get => _views [index];
			set
            {
                var item = _views[index];
                item.Parent = null;
                item.Navigation = null;
                var removed = new List<View> { item };

                _views[index] = value;

                value.Parent = null;
                value.Navigation = null;

                ChildrenChanged?.Invoke (this, new LayoutEventArgs (index, 1, removed));
			}
		}

		protected override void OnParentChange (View parent)
		{
			base.OnParentChange (parent);
			foreach (var view in _views)
            {
				view.Parent = this;
                view.Navigation = parent as NavigationView ?? parent?.Navigation;
            }
		}
		internal override void ContextPropertyChanged (string property, object value)
		{
			base.ContextPropertyChanged (property, value);
			foreach (var view in _views) {
				view.ContextPropertyChanged (property, value);
			}
		}
		
		public override RectangleF Frame
		{
			get => base.Frame;
			set
			{
				base.Frame = value;
				RequestLayout();
			}
		}
		
        public override SizeF Measure(SizeF availableSize)
        {
	        var width = FrameConstraints?.Width;
	        var height = FrameConstraints?.Height;

	        // If we have both width and height constraints, we can skip measuring the control and
	        // return the constrained values.
	        if (width != null && height != null)
		        return new SizeF((float)width, (float)height);

	        var measuredSize = _layout?.Measure(this, availableSize) ?? availableSize;
            
	        // If we have a constraint for just one of the values, then combine the constrained value
	        // with the measured value for our size.
	        if (width != null || height != null)
		        return new SizeF(width ?? measuredSize.Width, height ?? measuredSize.Height);

	        return measuredSize;
        }
        
        public override void LayoutSubviews(RectangleF bounds)
        {
	        _layout?.Layout(this, bounds.Size);
        }
    
        protected override void Dispose(bool disposing)
        {
            foreach (var view in _views)
            {
                view.Dispose();
            }
            _views.Clear();
            _layout.Invalidate();

            base.Dispose(disposing);
        }
	}
}