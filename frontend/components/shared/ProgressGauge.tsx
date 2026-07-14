import { GAUGE_STEPS, STATUS_LABEL_VI, type WorkOrderStatus } from "@/types/domain";
import styles from "./ProgressGauge.module.css";

interface ProgressGaugeProps {
  status: WorkOrderStatus;
  isDelayed?: boolean;
  delayReason?: string;
  onSteel?: boolean;
  size?: number;
}

const CX = 120;
const CY = 120;
const R = 96;

function pointAt(theta: number, radius: number) {
  const rad = (theta * Math.PI) / 180;
  return { x: CX + radius * Math.cos(rad), y: CY - radius * Math.sin(rad) };
}

// Đồng hồ đo tiến trình kiểu taplo ô tô (design.md §5.2) — thay progress bar thẳng.
export function ProgressGauge({ status, isDelayed = false, delayReason, onSteel = false, size = 240 }: ProgressGaugeProps) {
  // WaitingParts dùng chung vị trí với InRepair trên mặt đồng hồ.
  const effectiveStatus = status === "WaitingParts" ? "InRepair" : status;
  const stepIndex = GAUGE_STEPS.indexOf(effectiveStatus);
  const isCancelled = status === "Cancelled";

  const thetaFor = (i: number) => 180 - i * (180 / (GAUGE_STEPS.length - 1));
  const rotateDeg = stepIndex >= 0 ? 90 - thetaFor(stepIndex) : 0;

  const arcStart = pointAt(180, R);
  const arcEnd = pointAt(0, R);
  const needleColor = isDelayed ? "var(--brake-red)" : "var(--safety-amber)";

  return (
    <div className={`${styles.wrap} ${onSteel ? styles.onSteel : ""}`}>
      <svg width={size} height={size * 0.62} viewBox="0 0 240 150">
        <path
          d={`M ${arcStart.x} ${arcStart.y} A ${R} ${R} 0 0 1 ${arcEnd.x} ${arcEnd.y}`}
          fill="none"
          stroke={onSteel ? "rgba(237,239,238,0.18)" : "rgba(23,24,26,0.14)"}
          strokeWidth={10}
          strokeLinecap="round"
        />
        {GAUGE_STEPS.map((step, i) => {
          const theta = thetaFor(i);
          const tick = pointAt(theta, R);
          const label = pointAt(theta, R + 18);
          const passed = !isCancelled && i <= stepIndex;
          return (
            <g key={step}>
              <circle
                cx={tick.x}
                cy={tick.y}
                r={4}
                fill={passed ? needleColor : onSteel ? "#4B5257" : "#B9BDBA"}
              />
              <text x={label.x} y={label.y} textAnchor="middle" className={styles.tickLabel}>
                {STATUS_LABEL_VI[step].split(" ")[0]}
              </text>
            </g>
          );
        })}
        {!isCancelled && stepIndex >= 0 && (
          <g className={styles.needle} style={{ transform: `rotate(${rotateDeg}deg)` }}>
            <line x1={CX} y1={CY - 2} x2={CX} y2={CY - R + 14} stroke={needleColor} strokeWidth={4} strokeLinecap="round" />
            <circle cx={CX} cy={CY - 2} r={7} fill={needleColor} />
          </g>
        )}
      </svg>
      <div className={styles.currentLabel} style={{ color: isDelayed ? "var(--brake-red)" : undefined }}>
        {STATUS_LABEL_VI[status]}
      </div>
      {isDelayed && delayReason && <div className={styles.delayNote}>Gia hạn tới {delayReason}</div>}
    </div>
  );
}
