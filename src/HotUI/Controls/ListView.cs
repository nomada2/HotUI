﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HotUI {

    public interface IListView
    {
        int Sections();
        int Rows(int section);
        View FooterView();
        View HeaderView();
        View ViewFor(int section, int row);
        View HeaderFor(int section);
        View FooterFor(int section);
        bool ShouldDisposeViews { get; }
        void OnSelected(int section, int row);
    }

	public class ListView<T> : ListView
    {
        //TODO Evaluate if 30 is a good number
        FixedSizeDictionary<object, View> CurrentViews = new FixedSizeDictionary<object, View>(30);
        public readonly IList<T> List;
        public ListView (IList<T> list) : base()
		{
            List = list;
            CurrentViews.OnDequeue = (pair) => pair.Value?.Dispose();
        }

        public void Add(Func<T, View> cell)
        {
            if (Cell != null)
            {
                throw new Exception("You can only have one ListView Cell");
            }
            Cell = cell;
        }
        public View Header { get; set; }

        public View Footer { get; set; }

        public override void Add(View view)
        {
            throw new NotSupportedException();
        }

        public Func<T, View> Cell { get; set; }
	
        protected override int RowCount() => List?.Count ?? 0;
        protected override View ViewFor(int index)
        {
            var item = List[index];
            if (!CurrentViews.TryGetValue(item, out var view) || (view?.IsDisposed ?? true))
            {
                CurrentViews[item] = view = Cell(item);
                view.Parent = this;
            }
            return view;
        }
        protected override View HeaderView() => Header;
        protected override View FooterView() => Footer;

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            var currentViews = CurrentViews.ToList();
            CurrentViews.Clear();
            currentViews.ForEach(x => x.Value?.Dispose());
            base.Dispose(disposing);
        }

        protected override void OnParentChange(View parent)
        {
            base.OnParentChange(parent);
            foreach (var pair in CurrentViews)
                pair.Value.Parent = parent;
        }
        protected override void OnSelected(int index)
        {
            var item = List[index];
            var view = ViewFor(index);
            if (view is NavigationButton navigation)
            {
                navigation.Parent = this;
                navigation.Navigate();
                return;
            }

            ItemSelected?.Invoke(item);
        }

    }

    public class ListView : View, IEnumerable, IEnumerable<View>, IListView {

        List<View> views = new List<View>();
		public ListView()
		{
            ShouldDisposeView = false;
		}

		public virtual IEnumerator GetEnumerator () => views.GetEnumerator ();

		public virtual void Add(View view)
		{
            views.Add(view);
		}
        

        IEnumerator<View> IEnumerable<View>.GetEnumerator() => views.GetEnumerator();

        protected virtual View ViewFor(int section,int row) => ViewFor(row);
        protected virtual View ViewFor(int index) => views[index];

        public Action<object> ItemSelected { get; set; }
        protected virtual void OnSelected(int section, int index) => OnSelected(index);
        protected virtual void OnSelected (int index)
		{
            var view = views[index];
            if(view.IsDisposed)
            {
                Console.WriteLine(":(");
            }
            if(view is NavigationButton navigation)
            {
                navigation.Parent = this;
                navigation.Navigate();
                return;
            }
           
			ItemSelected?.Invoke (view);
		}
        protected virtual int RowCount() => views.Count;
		public void OnSelected (object item) => ItemSelected?.Invoke (item);

        protected override void Dispose(bool disposing)
        {
            views.ForEach(v => v.Dispose());
            //TODO: Verify. I don't think we need to check all active views anymore
            var cells = ActiveViews.Where(x => x.Parent == this).ToList();
            foreach (var cell in cells)
                cell.Dispose();
            base.Dispose(disposing);
        }
        protected override void OnParentChange(View parent)
        {
            base.OnParentChange(parent);
            views.ForEach(v => v.Parent = parent);
        }


        View IListView.ViewFor(int section, int row) => ViewFor(section,row);

        View IListView.HeaderFor(int section) => HeaderFor(section);

        View IListView.FooterFor(int section) => FooterFor(section);

        void IListView.OnSelected(int section, int row) => OnSelected(row);

        int IListView.Sections() => 1;

        int IListView.Rows(int section) => RowCount();

        View IListView.FooterView() => FooterView();

        View IListView.HeaderView() => HeaderView();


        protected virtual View FooterView() => null;

        protected virtual View HeaderView() => null;

        protected virtual View HeaderFor(int section) => null;
        protected virtual View FooterFor(int section) => null;
        protected bool ShouldDisposeView { get; set; }  = true;
        bool IListView.ShouldDisposeViews { get => ShouldDisposeView; }
    }
}
