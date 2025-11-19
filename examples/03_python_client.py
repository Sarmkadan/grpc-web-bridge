#!/usr/bin/env python3
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

"""
gRPC-Web Bridge - Python Client Examples

This module demonstrates how to communicate with the gRPC-Web Bridge
from a Python application.

Installation:
    pip install requests
"""

import json
import requests
import logging
from typing import Dict, Any, Optional
from dataclasses import dataclass

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


@dataclass
class Config:
    """Bridge client configuration"""
    bridge_url: str = "http://localhost:5000"
    token: Optional[str] = None
    timeout: int = 30
    verify_ssl: bool = True


class GrpcWebBridgeClient:
    """Client for communicating with gRPC-Web Bridge"""

    def __init__(self, config: Config):
        self.config = config
        self.session = requests.Session()
        self._setup_headers()

    def _setup_headers(self) -> None:
        """Setup default request headers"""
        headers = {
            'Content-Type': 'application/json',
            'User-Agent': 'gRPC-Web-Bridge-Python-Client/1.0'
        }

        if self.config.token:
            headers['Authorization'] = f'Bearer {self.config.token}'

        self.session.headers.update(headers)

    def set_token(self, token: str) -> None:
        """Update authentication token"""
        self.config.token = token
        self.session.headers['Authorization'] = f'Bearer {token}'

    def health_check(self) -> Dict[str, Any]:
        """Check bridge health status"""
        try:
            response = self.session.get(
                f'{self.config.bridge_url}/health',
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            logger.error(f'Health check failed: {e}')
            raise

    def list_services(self) -> Dict[str, Any]:
        """List all registered services"""
        try:
            response = self.session.get(
                f'{self.config.bridge_url}/api/services',
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            logger.error(f'Failed to list services: {e}')
            raise

    def register_service(self,
                        service_name: str,
                        address: str,
                        health_check: bool = False) -> Dict[str, Any]:
        """Register a new service"""
        payload = {
            'serviceName': service_name,
            'address': address,
            'healthCheck': health_check
        }

        try:
            response = self.session.post(
                f'{self.config.bridge_url}/api/services/register',
                json=payload,
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )
            response.raise_for_status()
            logger.info(f'Service {service_name} registered successfully')
            return response.json() if response.text else {'status': 'ok'}
        except requests.RequestException as e:
            logger.error(f'Failed to register service: {e}')
            raise

    def call_service(self,
                    service: str,
                    method: str,
                    request_data: Dict[str, Any]) -> Dict[str, Any]:
        """Call a service method"""
        url = f'{self.config.bridge_url}/api/bridge/{service}/{method}'

        try:
            response = self.session.post(
                url,
                json=request_data,
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )

            if response.status_code == 401:
                raise PermissionError('Authentication failed: Invalid or expired token')
            elif response.status_code == 403:
                raise PermissionError('Insufficient permissions')
            elif response.status_code == 400:
                raise ValueError(f'Bad request: {response.text}')

            response.raise_for_status()
            return response.json()

        except requests.RequestException as e:
            logger.error(f'Service call failed: {e}')
            raise

    def get_metrics(self) -> Dict[str, Any]:
        """Get bridge metrics"""
        try:
            response = self.session.get(
                f'{self.config.bridge_url}/api/metrics',
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            logger.error(f'Failed to get metrics: {e}')
            raise

    def get_active_streams(self) -> Dict[str, Any]:
        """Get information about active streams"""
        try:
            response = self.session.get(
                f'{self.config.bridge_url}/api/streams',
                timeout=self.config.timeout,
                verify=self.config.verify_ssl
            )
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            logger.error(f'Failed to get streams: {e}')
            raise


def example_basic_usage():
    """Basic usage example"""
    print('\n' + '='*60)
    print('Basic Usage Example')
    print('='*60)

    config = Config(bridge_url='http://localhost:5000')
    client = GrpcWebBridgeClient(config)

    try:
        # Check health
        logger.info('Checking bridge health...')
        health = client.health_check()
        print(f'✓ Health status: {health}')

        # List services
        logger.info('Listing services...')
        services = client.list_services()
        print(f'✓ Services: {services}')

        # Get metrics
        logger.info('Fetching metrics...')
        metrics = client.get_metrics()
        print(f'✓ Metrics: {metrics}')

    except Exception as e:
        logger.error(f'Error: {e}')


def example_service_registration():
    """Service registration example"""
    print('\n' + '='*60)
    print('Service Registration Example')
    print('='*60)

    config = Config(bridge_url='http://localhost:5000')
    client = GrpcWebBridgeClient(config)

    try:
        # Register a service
        logger.info('Registering service...')
        result = client.register_service(
            service_name='UserService',
            address='grpc://localhost:50051',
            health_check=False
        )
        print(f'✓ Service registered: {result}')

        # List all services
        logger.info('Listing all services...')
        services = client.list_services()
        print(f'✓ Available services: {json.dumps(services, indent=2)}')

    except Exception as e:
        logger.error(f'Error: {e}')


def example_rpc_call():
    """RPC call example"""
    print('\n' + '='*60)
    print('RPC Call Example')
    print('='*60)

    config = Config(bridge_url='http://localhost:5000')
    client = GrpcWebBridgeClient(config)

    try:
        # Make an RPC call
        logger.info('Making RPC call to UserService.GetUser...')
        result = client.call_service(
            service='UserService',
            method='GetUser',
            request_data={'id': 42}
        )
        print(f'✓ Response: {json.dumps(result, indent=2)}')

    except Exception as e:
        logger.error(f'Error: {e}')


def example_with_authentication():
    """Authentication example"""
    print('\n' + '='*60)
    print('Authentication Example')
    print('='*60)

    # In production, obtain the token from your auth provider
    token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'

    config = Config(
        bridge_url='http://localhost:5000',
        token=token
    )
    client = GrpcWebBridgeClient(config)

    try:
        logger.info('Making authenticated request...')
        services = client.list_services()
        print(f'✓ Authenticated access successful: {services}')

    except PermissionError as e:
        logger.error(f'Authentication error: {e}')
    except Exception as e:
        logger.error(f'Error: {e}')


def example_error_handling():
    """Error handling example"""
    print('\n' + '='*60)
    print('Error Handling Example')
    print('='*60)

    config = Config(bridge_url='http://localhost:5000')
    client = GrpcWebBridgeClient(config)

    # Example 1: Non-existent service
    try:
        logger.info('Calling non-existent service...')
        result = client.call_service(
            service='NonExistentService',
            method='DoSomething',
            request_data={}
        )
    except Exception as e:
        print(f'✓ Caught expected error: {type(e).__name__}: {e}')

    # Example 2: Invalid request
    try:
        logger.info('Calling with invalid request...')
        result = client.call_service(
            service='UserService',
            method='GetUser',
            request_data={'invalid_field': 'value'}
        )
    except ValueError as e:
        print(f'✓ Caught validation error: {e}')
    except Exception as e:
        print(f'✓ Caught error: {type(e).__name__}: {e}')


def example_streaming():
    """Streaming example using requests-sse or similar"""
    print('\n' + '='*60)
    print('Streaming Example')
    print('='*60)

    config = Config(bridge_url='http://localhost:5000')
    client = GrpcWebBridgeClient(config)

    try:
        logger.info('Starting server streaming...')
        # Note: For proper streaming, use requests.Response.iter_lines()
        # or a library like requests-sse for better streaming support

        response = client.session.post(
            f'{config.bridge_url}/api/bridge/UserService/ListUsers',
            json={'pageSize': 10},
            stream=True,
            timeout=config.timeout
        )

        response.raise_for_status()

        message_count = 0
        for line in response.iter_lines():
            if line:
                message_count += 1
                data = json.loads(line)
                print(f'✓ Received message {message_count}: {data}')

        print(f'✓ Stream completed ({message_count} messages)')

    except Exception as e:
        logger.error(f'Error: {e}')


def main():
    """Run all examples"""
    print('\n')
    print('gRPC-Web Bridge - Python Client Examples')
    print('='*60)

    try:
        example_basic_usage()
        example_service_registration()
        example_rpc_call()
        example_with_authentication()
        example_error_handling()
        example_streaming()

    except KeyboardInterrupt:
        logger.info('Examples interrupted by user')
    except Exception as e:
        logger.error(f'Fatal error: {e}', exc_info=True)
        return 1

    print('\n' + '='*60)
    print('Examples completed!')
    print('='*60 + '\n')
    return 0


if __name__ == '__main__':
    exit(main())
