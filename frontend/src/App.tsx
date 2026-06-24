import { Button } from '@/components/ui/button'
import './App.scss'

function App() {
  return (
    <main className="soundlens-shell">
      <section className="soundlens-shell__workspace">
        <header className="soundlens-shell__header">
          <div>
            <p className="soundlens-shell__eyebrow">SoundLens</p>
            <h1 className="soundlens-shell__title">
              Acoustic investigation workspace
            </h1>
          </div>
          <Button variant="outline" size="sm">Scaffold</Button>
        </header>

        <div className="soundlens-shell__panels">
          <section className="soundlens-shell__panel">
            <h2 className="soundlens-shell__panel-title">Files</h2>
            <p className="soundlens-shell__panel-copy">Multi-file upload starts here.</p>
          </section>
          <section className="soundlens-shell__panel">
            <h2 className="soundlens-shell__panel-title">Evidence</h2>
            <p className="soundlens-shell__panel-copy">DSP results stay traceable.</p>
          </section>
          <section className="soundlens-shell__panel">
            <h2 className="soundlens-shell__panel-title">Agent</h2>
            <p className="soundlens-shell__panel-copy">Answers cite measured data.</p>
          </section>
        </div>
      </section>
    </main>
  )
}

export default App
