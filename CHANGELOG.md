## Changelog

### 0.9.77 - 15/08/2024
* Added extension methods to query multiple indexed datoms at once. Results from the database are merge-joined into a
single list that is extremely fast to calculate. 
* Rewrote `ObserveDatoms` resulting in a roughly 1000x speedup when 10,000 datoms are being observed. The function now returns
`IObservable<IChangeSet<Datom, DatomKey>>` which allows for extremely efficient set-like operations on the datoms being observed
