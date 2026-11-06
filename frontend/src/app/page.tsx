import { Rules } from "@/components/Rules";
import { Tracker } from "@/components/Tracker";

// Link to your personal exercise spreadsheet. Set it in frontend/.env.local (git-ignored), not in code.
const exerciseDbUrl = process.env.NEXT_PUBLIC_EXERCISE_DB_URL;

export default function Home() {
  return (
    <main className="wrap">
      <header className="masthead">
        <p className="eyebrow">Home gym strength split · barbell, dumbbells, EZ bar, bench, rack</p>
        <h1>Hierro</h1>
      </header>

      <Tracker />

      <Rules />

      <footer>
        Exercise names and demo videos come from your{" "}
        {exerciseDbUrl ? (
          <a href={exerciseDbUrl} target="_blank" rel="noopener noreferrer">
            exercise database
          </a>
        ) : (
          "exercise database"
        )}
        . Weights are in pounds on a 45 lb barbell.
      </footer>
    </main>
  );
}
