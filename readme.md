DisposableVisualizer is a Roslyn based Visual Studio plugin that marks
IDisposable objects.

![Screenshot](screenshot.png)

It's often difficult to remember which objects implement IDisposable and not
disposing of them can have dire consequences. Marking the objects inside Visual
Studio gives a visual cue to the developer to help ensure they're handled.

## TODO
* Configurable?
* Mark fields/variables that have a type associated with being disposable.
* Make disposing of a Task in using() { } statements an error because it usually indicates a missing await.
* Update from the project template's default Core 1.0 + CodeAnalyzer 1.0 versions.

## Release log
### 1.1 (March 16h, 2018)
* Added nuget package to the release section.
* Ignore disposables constructed inside 'using' statements.
* Do not mark benign types such as MemoryStream and Task who's Dispose methods are (mostly) no-ops.
### 1.0 (March 11th, 2018)
* Initial release.