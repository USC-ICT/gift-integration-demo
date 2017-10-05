using UnityEngine;
using System.Collections;

public class ItemRifle : MonoBehaviour
{
    private ParticleSystem[] muzzleFlashEmitters;       //!< muzzle flash emitters
    private Light muzzleFlashLight;                      //!< muzzle light attached to gun
    private AudioSource muzzleAudioSource;               //!< audio source attached to gun
    private GameObject bulletOrigin;                     //!< spawn location of the bullets when the gun is fired
    public  AudioClip muzzleClip;                        //!< muzzleClip which is being used to play audio

    public  bool isAudioSingleBulletClip = true;         //!< if the audio clip being used is single-bullet shot or multi-bullet shot
    public  GameObject[] m_BulletDecals;
    private bool isStopRequired = false;                 //!< if pre-emptive stop is required.
    private bool isShooting = false;
	
	public Transform BulletStart
	{
        get { return bulletOrigin.transform; }
	}

    public void Start()
    {
        muzzleFlashEmitters = GetComponentsInChildren<ParticleSystem>();
        muzzleFlashLight = GetComponentInChildren<Light>();
        muzzleAudioSource = GetComponentInChildren<AudioSource>();
        bulletOrigin = VHUtils.FindChild(gameObject, "LocatorsAndEffects/BulletOrigin");
        if (bulletOrigin == null)
        {
            Debug.LogError("Can't find bullet origin on " + gameObject.name);
        }
        muzzleFlashLight.enabled = false;
    }

    public void Fire(int rounds, float rate) 
    {
        isStopRequired = false;
        if (isShooting == false)
        {
            StartCoroutine(Shoot(rounds, rate));
        }
    }

    IEnumerator Shoot(int rounds, float rate)
    {
        // if it's a multi-bullet audio clip, just play the sound once.
        if (isAudioSingleBulletClip == false && muzzleAudioSource.isPlaying == false)
        {
            muzzleAudioSource.Play();
        }

        // for number of rounds left emit particles, flash light, play sound (if single-clip) audio
        if (rounds > 0)
        {
            isShooting = true;
            if (isAudioSingleBulletClip == true)
            {
                muzzleAudioSource.PlayOneShot(muzzleClip);
            }

            if (muzzleFlashEmitters != null)
            {
                for (int i = 0; i < muzzleFlashEmitters.Length; i++)
                {
                    (muzzleFlashEmitters[i] as ParticleSystem).Emit(1);
                }
            }

            if (isStopRequired == false)
            {
                StartCoroutine(BlinkLight());
            }
            rounds -= 1;
            yield return new WaitForSeconds(rate);
            if (isStopRequired == false)
            {
                StartCoroutine(Shoot(rounds, rate));
            }
        }
        else
        {
            isShooting = false;
        }
    }

    IEnumerator BlinkLight()
    {
        muzzleFlashLight.enabled = true;
        yield return new WaitForSeconds(.01f);
        muzzleFlashLight.enabled = false;
    }

    public void Stop()
    {
        if (muzzleAudioSource.isPlaying)
        {
            muzzleAudioSource.Stop();
            isStopRequired = true;
            isShooting = false;
        }
        StopCoroutine("Shoot");
        StopCoroutine("BlinkLight");
    }
	
	public GameObject CreateBulletHole(Vector3 position, Vector3 facingDirection)
	{
		GameObject bulletHole = (GameObject)Instantiate(m_BulletDecals[Random.Range(0, m_BulletDecals.Length)]);
        bulletHole.transform.up = facingDirection;
        bulletHole.transform.position = position + facingDirection * 0.001f;
		return bulletHole;
	}
}
