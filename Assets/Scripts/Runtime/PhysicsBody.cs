using UnityEngine;

public class PhysicsBody : MonoBehaviour
{
    private Rigidbody _rb3D;
    private Rigidbody2D _rb2D;
    private bool _is2D;

    void Awake()
    {
        _rb3D = GetComponent<Rigidbody>();
        _rb2D = GetComponent<Rigidbody2D>();
        _is2D = _rb2D != null;
    }

    // --- CORE PROPERTIES ---

    public float mass
    {
        get => _is2D ? _rb2D.mass : _rb3D.mass;
        set { if (_is2D && _rb2D) _rb2D.mass = value; else if (!_is2D && _rb3D) _rb3D.mass = value; }
    }

    public float drag
    {
        get => _is2D ? _rb2D.drag : _rb3D.drag;
        set { if (_is2D && _rb2D) _rb2D.drag = value; else if (!_is2D && _rb3D) _rb3D.drag = value; }
    }

    public float angularDrag
    {
        get => _is2D ? _rb2D.angularDrag : _rb3D.angularDrag;
        set { if (_is2D && _rb2D) _rb2D.angularDrag = value; else if (!_is2D && _rb3D) _rb3D.angularDrag = value; }
    }

    public Vector3 velocity
    {
        get => _is2D ? (Vector3)_rb2D.velocity : _rb3D.velocity;
        set { if (_is2D && _rb2D) _rb2D.velocity = value; else if (!_is2D && _rb3D) _rb3D.velocity = value; }
    }

    public bool isKinematic
    {
        get => _is2D ? _rb2D.isKinematic : _rb3D.isKinematic;
        set { if (_is2D && _rb2D) _rb2D.isKinematic = value; else if (!_is2D && _rb3D) _rb3D.isKinematic = value; }
    }

    // Unity 2D uses gravityScale (float), 3D uses useGravity (bool). This cleanly bridges them!
    public bool useGravity
    {
        get => _is2D ? (_rb2D.gravityScale > 0) : _rb3D.useGravity;
        set
        {
            if (_is2D && _rb2D) _rb2D.gravityScale = value ? 1f : 0f;
            else if (!_is2D && _rb3D) _rb3D.useGravity = value;
        }
    }

    // --- CORE METHODS ---

    // Translates 3D AddForce into either 3D or 2D depending on the engine
    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        if (_is2D && _rb2D != null)
        {
            // Map 3D ForceMode to 2D ForceMode
            ForceMode2D mode2D = (mode == ForceMode.Impulse || mode == ForceMode.VelocityChange)
                ? ForceMode2D.Impulse
                : ForceMode2D.Force;

            _rb2D.AddForce(force, mode2D);
        }
        else if (!_is2D && _rb3D != null)
        {
            _rb3D.AddForce(force, mode);
        }
    }

    public Vector3 position
    {
        get => _is2D ? (Vector3)_rb2D.position : _rb3D.position;
        set { if (_is2D && _rb2D) _rb2D.position = value; else if (!_is2D && _rb3D) _rb3D.position = value; }
    }
}