using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Reflection;

namespace UTJ {
    /// <summary>
    /// 边缘方向显示
    /// Adjusted the following:
    /// https://forum.unity.com/threads/how-to-add-flow-effect-to-edges-in-graphview.1326012/
    /// </summary>
    public class FlowingEdge : Edge {
        //#region MEMBER
        private float _flowSize = 6f;
        private readonly Image flowImg;

        private float totalEdgeLength, passedEdgeLength, currentPhaseLength;
        private int phaseIndex;
        private double phaseStartTime, phaseDuration;

        private FieldInfo selectedColorField = null;
        private Color selectedDefaultColor;
        //#endregion


        //#region PROPERTY
        /// <summary>
        /// 点大小
        /// </summary>
        public float flowSize {
            get => this._flowSize;
            set {
                this._flowSize = value;
                this.flowImg.style.width = new Length(this._flowSize, LengthUnit.Pixel);
                this.flowImg.style.height = new Length(this._flowSize, LengthUnit.Pixel);
            }
        }
        /// <summary>
        /// 点移动速度
        /// </summary>
        public float flowSpeed { get; set; } = 150f;

        /// <summary>
        /// 点显示启用/禁用
        /// </summary>
        private bool __activeFlow;
        public bool activeFlow {
            get => __activeFlow;
            set {
                if (__activeFlow == value)
                    return;

                this.selected = __activeFlow = value;
                if (value) {
                    this.selectedDefaultColor = (Color)this.selectedColorField.GetValue(this);
                    this.Add(this.flowImg);
                    this.ResetFlowing();
                } else {
                    this.Remove(this.flowImg);
                    this.selectedColorField.SetValue(this, this.selectedDefaultColor);
                }
            }
        }
        //#endregion


        #region MAIN FUNCTION
        public FlowingEdge() {
            this.flowImg = new Image {
                name = "flow-image",
                style = {
                    width = new Length(flowSize, LengthUnit.Pixel),
                    height = new Length(flowSize, LengthUnit.Pixel),
                    borderTopLeftRadius = new Length(flowSize / 2, LengthUnit.Pixel),
                    borderTopRightRadius = new Length(flowSize / 2, LengthUnit.Pixel),
                    borderBottomLeftRadius = new Length(flowSize / 2, LengthUnit.Pixel),
                    borderBottomRightRadius = new Length(flowSize / 2, LengthUnit.Pixel),
                },
            };
            this.schedule.Execute(timer => { this.UpdateFlow(); }).Every(66); // 以 15fps 更新
            this.capabilities &= ~Capabilities.Deletable; //禁止边缘去除
            this.edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeControlGeometryChanged);

            // 本来是用CustomStyle设置的，但是比较麻烦，所以Reflection
            this.selectedColorField = typeof(Edge).GetField("m_SelectedColor", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// 回调，例如当 Edge 聚焦时
        /// </summary>
        /// <returns></returns>
        public override bool UpdateEdgeControl() {
            // 由于是内部返回，所以每次都设置
            // 一开始不打算改变颜色
            if (this.activeFlow)
                this.selectedColorField.SetValue(this, Color.green);
            return base.UpdateEdgeControl();
        }

        /// <summary>
        /// 定時更新
        /// </summary>
        private void UpdateFlow() {
            if (!this.activeFlow)
                return;

            // Position
            var posProgress = (float)((EditorApplication.timeSinceStartup - this.phaseStartTime) / this.phaseDuration);
            var flowStartPoint = this.edgeControl.controlPoints[phaseIndex];
            var flowEndPoint = this.edgeControl.controlPoints[phaseIndex + 1];
            var flowPos = Vector2.Lerp(flowStartPoint, flowEndPoint, posProgress);
            this.flowImg.transform.position = flowPos - Vector2.one * flowSize / 2;

            // Color
            var colorProgress = (this.passedEdgeLength + this.currentPhaseLength * posProgress) / this.totalEdgeLength;
            var startColor = this.edgeControl.outputColor;
            var endColor = this.edgeControl.inputColor;
            var flowColor = Color.Lerp(startColor, endColor, (float)colorProgress);
            this.flowImg.style.backgroundColor = flowColor;

            // Enter next phase
            if (posProgress >= 0.99999f) {
                this.passedEdgeLength += this.currentPhaseLength;

                this.phaseIndex++;
                if (this.phaseIndex >= this.edgeControl.controlPoints.Length - 1) {
                    // Restart flow
                    this.phaseIndex = 0;
                    this.passedEdgeLength = 0f;
                }

                this.phaseStartTime = EditorApplication.timeSinceStartup;
                this.currentPhaseLength = Vector2.Distance(this.edgeControl.controlPoints[phaseIndex], this.edgeControl.controlPoints[phaseIndex + 1]);
                this.phaseDuration = this.currentPhaseLength / this.flowSpeed;
            }
        }

        /// <summary>
        /// 转换边缘时的回调
        /// </summary>
        /// <param name="evt"></param>
        private void OnEdgeControlGeometryChanged(GeometryChangedEvent evt) {
            this.ResetFlowing();
        }

        /// <summary>
        /// 点坐标和距离重新计算
        /// </summary>
        private void ResetFlowing() {
            this.phaseIndex = 0;
            this.passedEdgeLength = 0f;
            this.phaseStartTime = EditorApplication.timeSinceStartup;
            this.currentPhaseLength = Vector2.Distance(this.edgeControl.controlPoints[phaseIndex], this.edgeControl.controlPoints[phaseIndex + 1]);
            this.phaseDuration = this.currentPhaseLength / this.flowSpeed;
            this.flowImg.transform.position = this.edgeControl.controlPoints[phaseIndex];

            // Calculate edge path length
            this.totalEdgeLength = 0;
            for (int i = 0; i < this.edgeControl.controlPoints.Length - 1; i++) {
                var p = this.edgeControl.controlPoints[i];
                var pNext = this.edgeControl.controlPoints[i + 1];
                var phaseLen = Vector2.Distance(p, pNext);
                this.totalEdgeLength += phaseLen;
            }

            if (this.activeFlow)
                this.selectedColorField.SetValue(this, Color.green);
        }
        #endregion
    }
}