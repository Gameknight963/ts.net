import "native.cr";

// Store<T> owns copyable graph records. IDs are one-based, with zero reserved
// for an absent link. Append-only IDs survive vector growth. Reads return copies
// so callers can recursively add records without retaining invalidated borrows.
struct Store<T> {
    private Vector<T> records;
    static Self create() { return (Self){.records = Vector<T>::create()}; }
    size_t count() const { return this.records.count(); }
    size_t add(T record) {
        this.records.push(record);
        return this.records.count();
    }
    T get(size_t id) const {
        if (id == 0 || id > this.records.count()) {
            print("tsnetc: invalid store ID");
            exit(1);
        }
        return this.records.at(id - 1);
    }
    void put(size_t id, T record) {
        if (id == 0 || id > this.records.count()) {
            print("tsnetc: invalid store ID");
            exit(1);
        }
        this.records.at_mut(id - 1) = record;
    }
    T& at_mut(size_t id) {
        if (id == 0 || id > this.records.count()) {
            print("tsnetc: invalid store ID");
            exit(1);
        }
        return this.records.at_mut(id - 1);
    }
};
