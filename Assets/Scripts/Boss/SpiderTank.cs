﻿using UnityEngine;

public class SpiderTank : MonoBehaviour
{
	public delegate void HealthTrigger( HealthSystem health );

	public Transform player;

	public Collider doorCollider;

	public Gun mainCanon;
	public BeamWeapon [] laserCanon;
	public Gun[] otherGuns;
	public MortarAttack mortarLauncher;
	public SpawnerMortarAttack spawnerLauncher;
	public GameObject shield;
	public float canonDelay;
	public AudioClip canonWarning;

	public float defaultCanonLookSpeed;
	public float healthTriggerInterval;

	public HealthCheckpoints healthCheckpoints;
	public StateSettings[] phaseSettings;

	[HideInInspector] public SpiderTankBasicState basicState;
	[HideInInspector] public SpiderTankFleeState fleeState;
	[HideInInspector] public SpiderTankHealState healState;
	[HideInInspector] public SpiderTankLaserSpin laserSpin;
	[HideInInspector] public SpiderTankRushState rushState;
	[HideInInspector] public SpiderTankTurboState turboState;
	[HideInInspector] public SpiderTankEnterState enterState;

	[HideInInspector] public HealthSystem health;
	[HideInInspector] public EnemySpawner spawner;
	[HideInInspector] public NavMeshAgent agent;
	[HideInInspector] public BoxCollider boxCollider;
	[HideInInspector] public Canvas ringUICanvas;
	[HideInInspector] public MeshRenderer[] meshes;

	[HideInInspector] public Animator animator;

	private HealthTrigger _healthTriggerCallback = delegate( HealthSystem health ) { };
	private float _healthMaxCurr;
	private float _healthMaxStart;
	private float _healthTrigger;

	void Awake()
	{
		// retrieve all states
		basicState = GetComponent<SpiderTankBasicState>();
		fleeState = GetComponent<SpiderTankFleeState>();
		healState = GetComponent<SpiderTankHealState>();
		laserSpin = GetComponent<SpiderTankLaserSpin>();
		rushState = GetComponent<SpiderTankRushState>();
		turboState = GetComponent<SpiderTankTurboState>();
		enterState = GetComponent<SpiderTankEnterState>();

		// retrieve other componenets
		health = GetComponent<HealthSystem>();
		spawner = GetComponent<EnemySpawner>();
		agent = GetComponent<NavMeshAgent>();
		boxCollider = GetComponent<BoxCollider>();
		ringUICanvas = GetComponentInChildren<Canvas>();
		meshes = GetComponentsInChildren<MeshRenderer>();
		animator = GetComponentInChildren<Animator>();

		// register for player death callback
		player.gameObject.GetComponent<DeathSystem>().RegisterDeathCallback( PlayerDeathCallback );

		// register for damage callbacks
		health.RegisterHealthCallback( SpiderDamageCallback );
		healthCheckpoints.currentPhase = 0;

		// set hand player over as the target to a bunch of script
		KeepDistance keepDistance = GetComponent<KeepDistance>();
		if ( keepDistance != null )
		{
			keepDistance.target = player;
		}
		mortarLauncher.mortarSettings.targets = new Transform[1];
		mortarLauncher.mortarSettings.targets[0] = player;
		spawnerLauncher.spiderTank = this;

		spawner.settings = phaseSettings[0].spawnerSettings;

		_healthMaxStart = health.maxHealth;
		_healthMaxCurr = _healthMaxStart;
	}

	void PlayerDeathCallback( GameObject gameObject )
	{
		// okay
		// this is going to sound crazy, but...
		// if the player dies after the boss dies,
		// this callback still gets called, and then this is null
		// and the call to GetComponent<>() fails because of a null
		// reference error. So we check to see if this is null before
		// trying to destroy the spider tank.
		if ( this != null )
		{
			GetComponent<DeathSystem>().Gut();
		}

		Destroy( mortarLauncher );
	}

	void SpiderDamageCallback( HealthSystem health, float damage )
	{
		if ( health.health < _healthTrigger )
		{
			_healthTriggerCallback( health );
		}

		int currentPhase = healthCheckpoints.currentPhase;
		if ( currentPhase < healthCheckpoints.phaseHealthPercents.Length - 1 )
		{
			float healthCheckpoint = healthCheckpoints.phaseHealthPercents[currentPhase + 1];
			if ( (health.percent * 100.0f) <= healthCheckpoint )
			{
				healthCheckpoints.currentPhase++;
				_healthMaxCurr = _healthMaxStart * healthCheckpoint * 0.01f;
				spawner.settings = phaseSettings[healthCheckpoints.currentPhase].spawnerSettings;
			}
		}
	}

	/**
	 * \brief Sets the current amount of health as the anchor point for the health trigger.
	 *
	 * \details The health trigger is activated when a certain portion of the boss's health
	 * has been lost, but the actual point should be a moving target based on an anchor, that is,
	 * the trigger is actived once a given percent of health has been lost relative to the anchor,
	 * rathar than relative to max health. This method set's the value of that anchor to the
	 * Spider Tank's current health.
	 */
	public void SetDamageBase()
	{
		_healthTrigger = health.health - healthTriggerInterval;
	}

	/**
	 * \brief Have the main canon look at the player gradually.
	 */
	public void LookMainCanon( float? lookSpeed = null )
	{
		Quaternion look = Quaternion.LookRotation( player.position - mainCanon.transform.position );
		mainCanon.transform.rotation = Quaternion.Lerp( mainCanon.transform.rotation, look, ( lookSpeed ?? defaultCanonLookSpeed ) * Time.deltaTime );
	}

	public void BeginMainCanon()
	{
		Invoke( "FireMainCanon", canonDelay );
		audio.clip = canonWarning;
		audio.Play();
		audio.volume = .8f;
		audio.priority = 0;
	}
	/**
	 * \brief Fire the main canon.
	 */
	public void FireMainCanon()
	{
		if ( !mainCanon.isOnCooldown )
		{
			mainCanon.PerformPrimaryAttack();
		}
	}

	/**
	 * \brief Have all other guns look at the player gradually.
	 */
	public void LookOtherGuns( float? lookSpeed = null )
	{
		foreach ( Gun gun in otherGuns )
		{
			Quaternion look = Quaternion.LookRotation( player.position - gun.transform.position );
			gun.transform.rotation = Quaternion.Lerp( gun.transform.rotation, look, ( lookSpeed ?? defaultCanonLookSpeed ) * Time.deltaTime );
		}
	}

	/**
	 * \brief Have all other guns fire.
	 */
	public void FireOtherGuns()
	{
		foreach ( Gun gun in otherGuns )
		{
			if ( !gun.isOnCooldown )
			{
				gun.PerformPrimaryAttack();
			}
		}
	}

	/**
	 * \brief Have the main canon and other guns look at the player gradually.
	 */
	public void LookAllGuns( float lookSpeed )
	{
		LookMainCanon( lookSpeed );
		LookOtherGuns( lookSpeed );
	}

	/**
	 * \brief Have the main canon and other guns fire.
	 */
	public void FireAllGuns()
	{
		BeginMainCanon();
		FireOtherGuns();
	}

	public void RegisterHealthTriggerCallback( HealthTrigger callback )
	{
		_healthTriggerCallback += callback;
	}

	public void DeregisterHealthTriggerCallback( HealthTrigger callback )
	{
		_healthTriggerCallback -= callback;
	}

	public int currentPhase
	{
		get
		{
			return healthCheckpoints.currentPhase;
		}
	}

	public bool atMaxHealth
	{
		get
		{
			return health.health >= _healthMaxCurr;
		}
	}
}


[System.Serializable]
public class HealthCheckpoints
{
	public float[] phaseHealthPercents = new float[]{ 100.0f, 75.0f, 50.0f, 25.0f };
	public int currentPhase;
}

[System.Serializable]
public class StateSettings
{
	public SpawnerSettings spawnerSettings;
	public MortarStateSettings mortarSettings;
}
