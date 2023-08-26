using System;
using TarodevController;
using TMPro;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [SerializeField] private Transform _player;
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private Vector3 _offset = new Vector3(0.75f, 2);
    [SerializeField] private float _lookAheadDistance = 2;
    [SerializeField] private float _lookAheadSpeed = 0;
    [SerializeField] private float _minY = 1.37f;

    private Vector3 _velOffset;
    private Vector3 _vel;
    private IPlayerController _playerController;
    private Vector3 _lookAheadVel;

    private void Awake() => _player.TryGetComponent(out _playerController);

    private void LateUpdate() {
        if (_playerController != null)
        {
            var projectedPos = _playerController.Velocity.normalized * _lookAheadDistance;
            _velOffset = Vector3.SmoothDamp(_velOffset, projectedPos, ref _lookAheadVel, _lookAheadSpeed);
        }

        Step(_smoothTime);
    }

    private void OnValidate() => Step(0);

    private void Step(float time)
    {
        // Adjusts offset when facing left or right
        if (_playerController != null && _playerController.Input.x > 0.1f) _offset[0] = Math.Abs(_offset[0]);
        else if (_playerController != null && _playerController.Input.x < -0.1f) _offset[0] = - Math.Abs(_offset[0]);

        var goal = _player.position + _offset + _velOffset;
        goal.z = -10;
        if (goal.y < _minY) goal.y = _minY;
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, time);
    }
}