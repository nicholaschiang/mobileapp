using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.Graphics;
using Android.Text;
using AndroidX.Core.Graphics;
using Toggl.Core.Calendar;
using Toggl.Core.UI.Calendar;
using Toggl.Core.UI.Collections;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHelpers;

namespace Toggl.Droid.Views.Calendar
{
    public partial class CalendarDayView
    {
        private readonly Dictionary<string, StaticLayout> textLayouts = new Dictionary<string, StaticLayout>();

        private float leftMargin;
        private float leftPadding;
        private float rightPadding;
        private float itemSpacing;
        private float minHourHeight;

        private int shortCalendarItemHeight;
        private int regularCalendarItemVerticalPadding;
        private int regularCalendarItemHorizontalPadding;
        private int shortCalendarItemVerticalPadding;
        private int shortCalendarItemHorizontalPadding;
        
        private RectF eventRect = new RectF();

        private readonly Paint eventsPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint textEventsPaint = new Paint(PaintFlags.AntiAlias);
        
        public void UpdateItems(ObservableGroupedOrderedCollection<CalendarItem> calendarItems)
        {
            var newItems = calendarItems.IsEmpty
                ? ImmutableList<CalendarItem>.Empty
                : calendarItems[0].ToImmutableList();
            
            updateItemsAndRecalculateEventsAttrs(newItems);
        }

        private void updateItemsAndRecalculateEventsAttrs(ImmutableList<CalendarItem> newItems)
        {
            if (availableWidth > 0)
            {
                calendarItemLayoutAttributes = calendarLayoutCalculator
                    .CalculateLayoutAttributes(newItems)
                    .Select(calculateCalendarItemRect)
                    .ToImmutableList();
            }
            calendarItems = newItems;
            PostInvalidate();
        }

        partial void initEventDrawingBackingFields()
        {
            minHourHeight = hourHeight / 4f;
            leftMargin = 68.DpToPixels(Context);
            leftPadding = 4.DpToPixels(Context);
            rightPadding = 4.DpToPixels(Context);
            itemSpacing = 4.DpToPixels(Context);
            availableWidth = Width - leftMargin;
            eventsPaint.SetStyle(Paint.Style.FillAndStroke);
            textEventsPaint.TextSize = 12.SpToPixels(Context);

            shortCalendarItemHeight = 18.DpToPixels(Context);
            regularCalendarItemVerticalPadding = 2.DpToPixels(Context);
            regularCalendarItemHorizontalPadding = 4.DpToPixels(Context);
            shortCalendarItemVerticalPadding = 0.5f.DpToPixels(Context);
            shortCalendarItemHorizontalPadding = 2.DpToPixels(Context);
        }

        partial void processEventsOnLayout(bool changed, int left, int top, int right, int bottom)
        {
            updateItemsAndRecalculateEventsAttrs(calendarItems);
        }

        private CalendarItemRectAttributes calculateCalendarItemRect(CalendarItemLayoutAttributes attrs)
        {
            var totalItemSpacing = (attrs.TotalColumns - 1) * itemSpacing;
            var eventWidth = (availableWidth - leftPadding - rightPadding - totalItemSpacing) / attrs.TotalColumns;
            var left = leftMargin + leftPadding + eventWidth * attrs.ColumnIndex + attrs.ColumnIndex * itemSpacing;
            
            return new CalendarItemRectAttributes(attrs, left, left + eventWidth);
        }

        partial void drawCalendarItems(Canvas canvas)
        {
            var itemsToDraw = calendarItems;
            var itemsAttrs = calendarItemLayoutAttributes;

            for (var eventIndex = 0; eventIndex < itemsAttrs.Count; eventIndex++)
            {
                var item = itemsToDraw[eventIndex];
                var itemAttr = itemsAttrs[eventIndex];

                itemAttr.CalculateRect(hourHeight, minHourHeight, eventRect);
                
                if (!(eventRect.Bottom > scrollOffset) || !(eventRect.Top - scrollOffset < Height)) continue;
                
                var color = Color.ParseColor(item.Color);
                if (item.Source == CalendarItemSource.Calendar)
                {
                    color.A = (byte)(color.A * 0.25);
                    color = new Color(ColorUtils.CompositeColors(color, ColorObject.White));
                }
                eventsPaint.Color = color;
                
                canvas.DrawRoundRect(eventRect, leftPadding / 2, leftPadding / 2, eventsPaint);
                drawCalendarItemText(canvas, item, eventRect);
            }
        }

        private void drawCalendarItemText(Canvas canvas, CalendarItem calendarItem, RectF calendarItemRect)
        {
            var eventHeight = calendarItemRect.Height();
            var eventWidth = calendarItemRect.Width();
            var fontSize = (eventHeight <= shortCalendarItemHeight ? 10 : 12).DpToPixels(Context);
            var textVerticalPadding = eventHeight <= shortCalendarItemHeight ? shortCalendarItemVerticalPadding : regularCalendarItemVerticalPadding;
            textVerticalPadding = (int) Math.Min((eventHeight - fontSize) / 2f, textVerticalPadding);
            var textHorizontalPadding = eventHeight <= shortCalendarItemHeight ? shortCalendarItemHorizontalPadding : regularCalendarItemHorizontalPadding;

            var eventTextLayout = getCalendarItemTextLayout(calendarItem, eventWidth, fontSize);
            var totalLineHeight = calculateLineHeight(eventHeight, eventTextLayout);

            canvas.Save();
            canvas.Translate(calendarItemRect.Left + textHorizontalPadding, calendarItemRect.Top + textVerticalPadding);
            canvas.ClipRect(0, 0, eventWidth, totalLineHeight);
            eventTextLayout.Draw(canvas);
            canvas.Restore();
        }

        private static int calculateLineHeight(double eventHeight, StaticLayout eventTextLayout)
        {
            var totalLineHeight = 0;
            for (var i = 0; i < eventTextLayout.LineCount; i++)
            {
                var lineBottom = eventTextLayout.GetLineBottom(i);
                if (lineBottom <= eventHeight)
                {
                    totalLineHeight = lineBottom;
                }
            }

            return totalLineHeight;
        }

        private StaticLayout getCalendarItemTextLayout(CalendarItem item, float eventWidth, int fontSize)
        {
            textLayouts.TryGetValue(item.Id, out var eventTextLayout);
            if (eventTextLayout != null && !(Math.Abs(eventTextLayout.Width - eventWidth) > 0.1) && eventTextLayout.Text == item.Description) 
                return eventTextLayout;
            
            var color = item.Source == CalendarItemSource.Calendar ? Color.ParseColor(item.Color) : Color.White;
            textEventsPaint.Color = color;
            textEventsPaint.TextSize = fontSize;
            
            eventTextLayout = new StaticLayout(item.Description,
                0,
                item.Description.Length,
                new TextPaint(textEventsPaint),
                (int) eventWidth,
                Android.Text.Layout.Alignment.AlignNormal,
                1.0f,
                0.0f,
                true,
                TextUtils.TruncateAt.End,
                (int) eventWidth);
            textLayouts[item.Id] = eventTextLayout;

            return eventTextLayout;
        }
    }
}