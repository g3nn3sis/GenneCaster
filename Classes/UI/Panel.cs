using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GenneCaster.Classes.UI
{
    public enum PanelAreaDirection
    {
        Vertical,
        Horizontal
    }

    public sealed class Panel : IDisposable
    {
        private static int nextWindowId;

        private readonly int windowId;
        private readonly string title;
        private readonly Stack<PanelAreaDirection> areaDirections =
            new Stack<PanelAreaDirection>();
        private static readonly GUILayoutOption[] expandWidthOptions = { GUILayout.ExpandWidth(true) };
        private readonly GUIContent measuredContent = new GUIContent();

        private Rect rect;

        private Texture2D backgroundTexture;
        private Texture2D buttonTexture;
        private Texture2D textFieldTexture;
        private Texture2D closeButtonTexture;
        private Texture2D closeButtonHoverTexture;

        private GUIStyle buttonStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle closeButtonStyle;
        private GUIStyle titleStyle;

        private Color32 backgroundColor;
        private Color32 borderColor;
        private Color32 buttonColor;
        private Color32 buttonBorderColor;
        private Color32 textFieldColor;
        private Color32 textFieldBorderColor;

        private Action<Panel> content;

        private bool enabled;
        private bool stylesInitialized;
        private bool disposed;

        public event Action EnabledChanged;
        public event Action Closed;

        public Panel(
            float x,
            float y,
            float width,
            float height,
            string title,
            Color32? accent = null)
        {
            if (width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(height));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException(
                    "Panel title cannot be empty.",
                    nameof(title)
                );

            windowId = Interlocked.Increment(ref nextWindowId);

            rect = new Rect(x, y, width, height);

            this.title = title;

            backgroundColor = new Color32(10, 10, 10, 125);
            borderColor = accent ?? new Color32(255, 255, 255, 255);

            buttonColor = new Color32(20, 5, 8, 190);
            buttonBorderColor = accent ?? new Color32(65, 65, 65, 255);

            textFieldColor = new Color32(20, 20, 20, 230);
            textFieldBorderColor = accent ?? new Color32(65, 65, 65, 255);

            enabled = true;

            CreateTextures();
        }

        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                SetEnabled(value);
            }
        }

        public Rect Rect
        {
            get
            {
                return rect;
            }

            set
            {
                ThrowIfDisposed();

                if (value.width <= 0f || value.height <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value));

                bool sizeChanged =
                    !Mathf.Approximately(rect.width, value.width) ||
                    !Mathf.Approximately(rect.height, value.height);

                rect = value;

                if (sizeChanged)
                    RebuildBackgroundTexture();
            }
        }

        public float X
        {
            get
            {
                return rect.x;
            }

            set
            {
                rect.x = value;
            }
        }

        public float Y
        {
            get
            {
                return rect.y;
            }

            set
            {
                rect.y = value;
            }
        }

        public float Width
        {
            get
            {
                return rect.width;
            }
        }

        public float Height
        {
            get
            {
                return rect.height;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        private void CreateTextures()
        {
            backgroundTexture = Textures.CreateTexture(
                Mathf.Max(1, (int)rect.width),
                Mathf.Max(1, (int)rect.height),
                backgroundColor,
                borderColor,
                borderThickness: 2,
                cornerRadius: 15
            );

            buttonTexture = Textures.CreateTexture(
                128,
                22,
                buttonColor,
                buttonBorderColor,
                borderThickness: 1,
                cornerRadius: 4
            );

            textFieldTexture = Textures.CreateTexture(
                128,
                22,
                textFieldColor,
                textFieldBorderColor,
                borderThickness: 1,
                cornerRadius: 4
            );

            closeButtonTexture = Textures.CreateTexture(
                16,
                16,
                new Color32(115, 25, 25, 235),
                new Color32(175, 50, 50, 255),
                borderThickness: 1,
                cornerRadius: 4
            );

            closeButtonHoverTexture = Textures.CreateTexture(
                16,
                16,
                new Color32(190, 35, 35, 255),
                new Color32(235, 75, 75, 255),
                borderThickness: 1,
                cornerRadius: 4
            );
        }

        public void Draw(Action<Panel> drawContent)
        {
            ThrowIfDisposed();

            if (!enabled)
                return;

            if (drawContent == null)
                throw new ArgumentNullException(nameof(drawContent));

            content = drawContent;

            rect = GUI.Window(
                windowId,
                rect,
                DrawWindow,
                GUIContent.none,
                GUIStyle.none
            );
        }

        private void DrawWindow(int id)
        {
            EnsureStyles();

            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    rect.width,
                    rect.height
                ),
                backgroundTexture
            );

            float inset = 10f;

            float headerHeight = Mathf.Clamp(
                rect.height * 0.12f,
                23f,
                28f
            );

            float headerY = 5f;

            DrawHeader(
                inset,
                headerY,
                headerHeight
            );

            float contentY =
                headerY +
                headerHeight +
                2f;

            float contentWidth =
                rect.width -
                inset * 2f;

            float contentHeight =
                rect.height -
                contentY -
                inset;

            if (contentWidth > 0f && contentHeight > 0f)
            {
                GUILayout.BeginArea(
                    new Rect(
                        inset,
                        contentY,
                        contentWidth,
                        contentHeight
                    )
                );

                try
                {
                    GUILayout.BeginVertical(
                        GUILayout.ExpandWidth(true)
                    );

                    areaDirections.Push(
                        PanelAreaDirection.Vertical
                    );

                    try
                    {
                        if (content != null)
                            content(this);
                    }
                    finally
                    {
                        areaDirections.Pop();
                        GUILayout.EndVertical();
                    }
                }
                finally
                {
                    GUILayout.EndArea();
                }
            }

            float closeSize = Mathf.Clamp(
                rect.width * 0.12f,
                15f,
                18f
            );

            float closeX =
                rect.width -
                inset -
                closeSize;

            GUI.DragWindow(
                new Rect(
                    inset,
                    headerY,
                    Mathf.Max(
                        1f,
                        closeX -
                        inset -
                        3f
                    ),
                    headerHeight
                )
            );
        }

        private void DrawHeader(
            float inset,
            float headerY,
            float headerHeight)
        {
            float closeSize = Mathf.Clamp(
                rect.width * 0.12f,
                15f,
                18f
            );

            float closeX =
                rect.width -
                inset -
                closeSize;

            float closeY =
                headerY +
                (headerHeight - closeSize) * 0.5f;

            titleStyle.fontSize = Mathf.RoundToInt(
                Mathf.Clamp(
                    rect.width / 11.5f,
                    10f,
                    12f
                )
            );

            GUI.Label(
                new Rect(
                    inset + 3f,
                    headerY,
                    Mathf.Max(
                        1f,
                        closeX -
                        inset -
                        6f
                    ),
                    headerHeight
                ),
                title,
                titleStyle
            );

            closeButtonStyle.fontSize = Mathf.RoundToInt(
                Mathf.Clamp(
                    closeSize * 0.65f,
                    9f,
                    11f
                )
            );

            if (!GUI.Button(
                    new Rect(
                        closeX,
                        closeY,
                        closeSize,
                        closeSize
                    ),
                    "×",
                    closeButtonStyle))
            {
                return;
            }

            Disable();

            if (Closed != null)
                Closed();
        }

        public void Area(
            PanelAreaDirection direction,
            Action draw,
            params GUILayoutOption[] options)
        {
            ThrowIfDisposed();

            if (draw == null)
                throw new ArgumentNullException(nameof(draw));

            GUILayoutOption[] finalOptions =
                WithExpandWidth(options);

            areaDirections.Push(direction);

            try
            {
                if (direction ==
                    PanelAreaDirection.Horizontal)
                {
                    GUILayout.BeginHorizontal(
                        finalOptions
                    );

                    try
                    {
                        draw();
                    }
                    finally
                    {
                        GUILayout.EndHorizontal();
                    }

                    return;
                }

                GUILayout.BeginVertical(
                    finalOptions
                );

                try
                {
                    draw();
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            finally
            {
                areaDirections.Pop();
            }
        }

        public void Area(
            Action draw,
            params GUILayoutOption[] options)
        {
            VerticalArea(
                draw,
                options
            );
        }

        public void VerticalArea(
            Action draw,
            params GUILayoutOption[] options)
        {
            Area(
                PanelAreaDirection.Vertical,
                draw,
                options
            );
        }

        public void HorizontalArea(
            Action draw,
            params GUILayoutOption[] options)
        {
            Area(
                PanelAreaDirection.Horizontal,
                draw,
                options
            );
        }

        public void CreateButton(
            string text,
            Action method,
            params GUILayoutOption[] options)
        {
            ThrowIfDisposed();
            EnsureStyles();

            string value =
                text ?? string.Empty;

            float height =
                GetControlHeight();

            Rect layoutRect =
                GUILayoutUtility.GetRect(
                    0f,
                    10000f,
                    height,
                    height,
                    WithExpandWidth(options)
                );

            Rect drawRect =
                InsetControlRect(layoutRect);

            int preferredSize =
                Mathf.RoundToInt(
                    Mathf.Clamp(
                    rect.width / 12.5f,
                    9f,
                    11f
                    )
                );

            buttonStyle.wordWrap = false;
            buttonStyle.clipping = TextClipping.Clip;
            buttonStyle.fontSize =
                GetFittingFontSize(
                    value,
                    Mathf.Max(
                        1f,
                        drawRect.width - 5f
                    ),
                    preferredSize,
                    7
                );

            if (GUI.Button(
                    drawRect,
                    value,
                    buttonStyle))
            {
                if (method != null)
                    method();
            }
        }

        public string CreateTextField(
            string text,
            params GUILayoutOption[] options)
        {
            ThrowIfDisposed();
            EnsureStyles();

            float height =
                GetControlHeight();

            Rect layoutRect =
                GUILayoutUtility.GetRect(
                    0f,
                    10000f,
                    height,
                    height,
                    WithExpandWidth(options)
                );

            Rect drawRect =
                InsetControlRect(layoutRect);

            textFieldStyle.fontSize =
                Mathf.RoundToInt(
                    Mathf.Clamp(
                        rect.width / 12.5f,
                        9f,
                        11f
                    )
                );

            return GUI.TextField(
                drawRect,
                text ?? string.Empty,
                textFieldStyle
            );
        }

        public string CreateTextFieldUpper(
            string text,
            params GUILayoutOption[] options)
        {
            string value =
                text ?? string.Empty;

            value = CreateTextField(
                value.ToUpperInvariant(),
                options
            );

            return value.ToUpperInvariant();
        }

        public string CreateTextFieldLower(
            string text,
            params GUILayoutOption[] options)
        {
            string value =
                text ?? string.Empty;

            value = CreateTextField(
                value.ToLowerInvariant(),
                options
            );

            return value.ToLowerInvariant();
        }

        public void Space(float pixels = 4f)
        {
            ThrowIfDisposed();

            GUILayout.Space(
                Mathf.Max(
                    0f,
                    pixels
                )
            );
        }

        public void ButtonSpace()
        {
            ButtonSpace(1f);
        }

        public void ButtonSpace(float amount)
        {
            ThrowIfDisposed();

            if (amount <= 0f)
                return;

            GUILayout.Space(
                GetControlHeight() * amount
            );
        }

        public void HalfButtonSpace()
        {
            ButtonSpace(0.5f);
        }

        public void FlexibleSpace()
        {
            ThrowIfDisposed();

            GUILayout.FlexibleSpace();
        }

        public void Enable()
        {
            SetEnabled(true);
        }

        public void Disable()
        {
            SetEnabled(false);
        }

        public void Toggle()
        {
            ThrowIfDisposed();

            SetEnabled(!enabled);
        }

        private void SetEnabled(bool value)
        {
            ThrowIfDisposed();

            if (enabled == value)
                return;

            enabled = value;

            if (EnabledChanged != null)
                EnabledChanged();
        }

        public void SetPosition(
            float x,
            float y)
        {
            ThrowIfDisposed();

            rect.x = x;
            rect.y = y;
        }

        public void SetBackgroundColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (backgroundColor.Equals(color))
                return;

            backgroundColor = color;

            RebuildBackgroundTexture();
        }

        public void SetBorderColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (borderColor.Equals(color))
                return;

            borderColor = color;

            RebuildBackgroundTexture();
        }

        public void SetButtonColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (buttonColor.Equals(color))
                return;

            buttonColor = color;

            RebuildButtonTexture();
        }

        public void SetButtonBorderColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (buttonBorderColor.Equals(color))
                return;

            buttonBorderColor = color;

            RebuildButtonTexture();
        }

        public void SetTextFieldColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (textFieldColor.Equals(color))
                return;

            textFieldColor = color;

            RebuildTextFieldTexture();
        }

        public void SetTextFieldBorderColor(
            Color32 color)
        {
            ThrowIfDisposed();

            if (textFieldBorderColor.Equals(color))
                return;

            textFieldBorderColor = color;

            RebuildTextFieldTexture();
        }

        private float GetControlHeight()
        {
            return Mathf.Clamp(Mathf.Min(rect.height * 0.105f, rect.width * 0.16f), 18f, 23f);
        }

        private Rect InsetControlRect(
            Rect source)
        {
            float horizontalInset =
                Mathf.Clamp(
                    rect.width * 0.009f,
                    1f,
                    2f
                );

            float verticalInset =
                Mathf.Clamp(
                    GetControlHeight() * 0.05f,
                    1f,
                    1.5f
                );

            return new Rect(
                source.x + horizontalInset,
                source.y + verticalInset,
                Mathf.Max(
                    1f,
                    source.width -
                    horizontalInset * 2f
                ),
                Mathf.Max(
                    1f,
                    source.height -
                    verticalInset * 2f
                )
            );
        }

        private int GetFittingFontSize(
            string text,
            float availableWidth,
            int preferredSize,
            int minimumSize)
        {
            if (string.IsNullOrEmpty(text))
                return preferredSize;

            measuredContent.text = text;

            for (
                int size = preferredSize;
                size >= minimumSize;
                size--)
            {
                buttonStyle.fontSize =
                    size;

                if (buttonStyle
                        .CalcSize(measuredContent)
                        .x <= availableWidth)
                {
                    return size;
                }
            }

            return minimumSize;
        }

        private static GUILayoutOption[] WithExpandWidth(
            GUILayoutOption[] options)
        {
            if (options == null ||
                options.Length == 0)
            {
                return expandWidthOptions;
            }

            GUILayoutOption[] result =
                new GUILayoutOption[
                    options.Length + 1
                ];

            Array.Copy(
                options,
                result,
                options.Length
            );

            result[result.Length - 1] =
                GUILayout.ExpandWidth(true);

            return result;
        }

        private void RebuildBackgroundTexture()
        {
            Texture2D oldTexture =
                backgroundTexture;

            backgroundTexture =
                Textures.CreateTexture(
                    Mathf.Max(
                        1,
                        (int)rect.width
                    ),
                    Mathf.Max(
                        1,
                        (int)rect.height
                    ),
                    backgroundColor,
                    borderColor,
                    borderThickness: 2,
                    cornerRadius: 15
                );

            DestroyTexture(oldTexture);
        }

        private void RebuildButtonTexture()
        {
            Texture2D oldTexture = buttonTexture;

            buttonTexture =
                Textures.CreateTexture(
                    128,
                    22,
                    buttonColor,
                    buttonBorderColor,
                    borderThickness: 1,
                    cornerRadius: 4
                );

            DestroyTexture(oldTexture);

            if (stylesInitialized)
            {
                ApplyBackground(buttonStyle, buttonTexture);
            }
        }

        private void RebuildTextFieldTexture()
        {
            Texture2D oldTexture =
                textFieldTexture;

            textFieldTexture =
                Textures.CreateTexture(
                    128,
                    22,
                    textFieldColor,
                    textFieldBorderColor,
                    borderThickness: 1,
                    cornerRadius: 4
                );

            DestroyTexture(oldTexture);

            if (stylesInitialized)
            {
                ApplyBackground(
                    textFieldStyle,
                    textFieldTexture
                );
            }
        }

        private void EnsureStyles()
        {
            if (stylesInitialized)
                return;

            buttonStyle =
                new GUIStyle(
                    GUI.skin.button
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    stretchWidth = true,

                    padding =
                        new RectOffset(
                            2,
                            2,
                            1,
                            1
                        ),

                    margin =
                        new RectOffset(
                            0,
                            0,
                            0,
                            0
                        ),

                    border =
                        new RectOffset(
                            4,
                            4,
                            4,
                            4
                        )
                };

            ApplyBackground(buttonStyle, buttonTexture);

            ApplyTextColor(
                buttonStyle,
                Color.white
            );

            textFieldStyle =
                new GUIStyle(
                    GUI.skin.textField
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    stretchWidth = true,

                    padding =
                        new RectOffset(
                            2,
                            2,
                            1,
                            1
                        ),

                    margin =
                        new RectOffset(
                            0,
                            0,
                            0,
                            0
                        ),

                    border =
                        new RectOffset(
                            4,
                            4,
                            4,
                            4
                        )
                };

            ApplyBackground(
                textFieldStyle,
                textFieldTexture
            );

            ApplyTextColor(
                textFieldStyle,
                Color.white
            );

            closeButtonStyle =
                new GUIStyle(
                    GUI.skin.button
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    fontStyle =
                        FontStyle.Bold,

                    padding =
                        new RectOffset(
                            0,
                            0,
                            0,
                            1
                        ),

                    margin =
                        new RectOffset(
                            0,
                            0,
                            0,
                            0
                        ),

                    border =
                        new RectOffset(
                            4,
                            4,
                            4,
                            4
                        )
                };

            closeButtonStyle.normal.background =
                closeButtonTexture;

            closeButtonStyle.focused.background =
                closeButtonTexture;

            closeButtonStyle.hover.background =
                closeButtonHoverTexture;

            closeButtonStyle.active.background =
                closeButtonHoverTexture;

            closeButtonStyle.onNormal.background =
                closeButtonTexture;

            closeButtonStyle.onFocused.background =
                closeButtonTexture;

            closeButtonStyle.onHover.background =
                closeButtonHoverTexture;

            closeButtonStyle.onActive.background =
                closeButtonHoverTexture;

            ApplyTextColor(
                closeButtonStyle,
                Color.white
            );

            titleStyle =
                new GUIStyle(
                    GUI.skin.label
                )
                {
                    alignment =
                        TextAnchor.MiddleLeft,

                    richText = true,

                    fontStyle =
                        FontStyle.Normal,

                    padding =
                        new RectOffset(
                            0,
                            0,
                            0,
                            0
                        ),

                    margin =
                        new RectOffset(
                            0,
                            0,
                            0,
                            0
                        ),

                    clipping =
                        TextClipping.Clip
                };

            ApplyTextColor(
                titleStyle,
                Color.white
            );

            stylesInitialized = true;
        }

        private static void ApplyBackground(
            GUIStyle style,
            Texture2D texture)
        {
            style.normal.background =
                texture;

            style.hover.background =
                texture;

            style.active.background =
                texture;

            style.focused.background =
                texture;

            style.onNormal.background =
                texture;

            style.onHover.background =
                texture;

            style.onActive.background =
                texture;

            style.onFocused.background =
                texture;
        }

        private static void ApplyTextColor(
            GUIStyle style,
            Color color)
        {
            style.normal.textColor =
                color;

            style.hover.textColor =
                color;

            style.active.textColor =
                color;

            style.focused.textColor =
                color;

            style.onNormal.textColor =
                color;

            style.onHover.textColor =
                color;

            style.onActive.textColor =
                color;

            style.onFocused.textColor =
                color;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(
                    nameof(Panel)
                );
        }

        private static void DestroyTexture(
            Texture2D texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(
                    texture
                );
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            enabled = false;

            DestroyTexture(
                backgroundTexture
            );

            DestroyTexture(
                buttonTexture
            );

            DestroyTexture(
                textFieldTexture
            );

            DestroyTexture(
                closeButtonTexture
            );

            DestroyTexture(
                closeButtonHoverTexture
            );

            backgroundTexture = null;
            buttonTexture = null;
            textFieldTexture = null;
            closeButtonTexture = null;
            closeButtonHoverTexture = null;

            buttonStyle = null;
            textFieldStyle = null;
            closeButtonStyle = null;
            titleStyle = null;

            content = null;

            EnabledChanged = null;
            Closed = null;

            areaDirections.Clear();
        }
    }
}
