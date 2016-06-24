using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using FtApp.Utils;
using System;

namespace FtApp.Droid.Views
{
    /// <summary>
    /// The JoystickView displays a jostick which can be used to control something
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class JoystickView : View
    {
        public delegate void ValuesChangedEventHandler(object sender, EventArgs eventArgs);

        public event ValuesChangedEventHandler ValuesChanged;

        public int ThumbSize { get; set; } = 35;

        private Drawable _background;
        private Drawable _thumb;
        
        private int _thumbPositionX;
        private int _thumbPositionY;

        public float ThumbDistance { get; private set; }
        public float ThumbAngle { get; private set; }
        public float ThumbX { get; private set; }
        public float ThumbY { get; private set; }

        public JoystickView(Context context) : base(context)
        {
            Initialize();
        }

        public JoystickView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
            int widthSize = MeasureSpec.GetSize(widthMeasureSpec);

            var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
            int heightSize = MeasureSpec.GetSize(heightMeasureSpec);

            int width;
            int height;
            
            switch (widthMode)
            {
                case MeasureSpecMode.Exactly:
                case MeasureSpecMode.AtMost:
                    width = widthSize;
                    break;
                default:
                    width = 300;
                    break;
            }
            
            switch (heightMode)
            {
                case MeasureSpecMode.Exactly:
                case MeasureSpecMode.AtMost:
                    height = heightSize;
                    break;
                default:
                    height = 300;
                    break;
            }

            int size = Math.Min(width, height);

            _thumbPositionX = size / 2;
            _thumbPositionY = size / 2;

            SetMeasuredDimension(size, size);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            _background.SetBounds(0, 0, Width, Height);
            _background.Draw(canvas);

            int x = _thumbPositionX - ThumbSize;
            int y = _thumbPositionY - ThumbSize;
            
            _thumb.SetBounds(x, y, _thumbPositionX + ThumbSize, _thumbPositionY + ThumbSize);
            _thumb.Draw(canvas);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                    CalculateThumbPosition(e.GetX(), e.GetY());
                    Invalidate();
                    break;

                case MotionEventActions.Up:
                    ResetThumbPosition();
                    Invalidate();
                    break;
            }

            return true;
        }

        private void ResetThumbPosition()
        {
            CalculateThumbPosition(Width / 2f, Width / 2f);
        }

        private void CalculateThumbPosition(float touchX, float touchY)
        {
            float thumbRelativeX = touchX - Width/2f;
            float thumbRelativeY = touchY - Height/2f;

            float thumbDistance = (float) Math.Sqrt(Math.Pow(thumbRelativeX, 2) + Math.Pow(thumbRelativeY, 2));
            ThumbAngle = CalculateAngle(thumbRelativeX, thumbRelativeY);

            if (thumbDistance > Width/2f-ThumbSize)
            {
                thumbDistance = Width/2f - ThumbSize;
            }

            _thumbPositionX = (int) (Math.Cos(MathUtils.ToRadians(ThumbAngle))* thumbDistance) + Width/2;
            _thumbPositionY = (int) (Math.Sin(MathUtils.ToRadians(ThumbAngle))* thumbDistance) + Height/2;

            // Normalize to percentage
            ThumbDistance = 100f/(Width/2f)*thumbDistance;

            ThumbX = 100f / (Width / 2f) * (float)(Math.Cos(MathUtils.ToRadians(ThumbAngle)) * thumbDistance) * 2f;
            ThumbY = 100f / (Width / 2f) * (float)(Math.Sin(MathUtils.ToRadians(ThumbAngle)) * thumbDistance) * 2f;
            
            ValuesChanged?.Invoke(this, EventArgs.Empty);
        }

        private float CalculateAngle(float x, float y)
        {
            float angle = 0;
            if (x >= 0 && y >= 0)
            {
                angle = (float)MathUtils.ToDegrees(Math.Atan(y / x));
            }
            if (x < 0 && y >= 0)
            {
                angle = (float)MathUtils.ToDegrees(Math.Atan(y / x)) + 180;
            }
            if (x < 0 && y < 0)
            {
                angle = (float)MathUtils.ToDegrees(Math.Atan(y / x)) + 180;
            }
            if (x >= 0 && y < 0)
            {
                angle = (float)MathUtils.ToDegrees(Math.Atan(y / x)) + 360;
            }
            
            return angle;
        }

        private void Initialize()
        {
            _background = Context.Resources.GetDrawable(Resource.Drawable.JoystickBackground);
            _thumb = Context.Resources.GetDrawable(Resource.Drawable.JoystickThumb);
        }
    }
}